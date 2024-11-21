using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace ChuteBlockPlacer
{
    public class BlockEntityPlacerItemFlow : BlockEntityOpenableContainer
    {
        public override AssetLocation OpenSound => new("game:sounds/block/hopperopen");
        public override AssetLocation CloseSound => null;

        public override InventoryBase Inventory => inventory;
        private InventoryGeneric inventory;

        private string inventoryClassName = "ChuteBlockPlacer";
        public override string InventoryClassName => inventoryClassName;

        private void InitInventory()
        {
            ParseBlockProperties();
            if (inventory != null) return;
            inventory = new(QuantitySlots, null, null, OnNewSlot)
            {
                OnGetAutoPushIntoSlot = GetAutoPushIntoSlot,
                OnGetAutoPullFromSlot = GetAutoPullFromSlot
            };

            inventory.SlotModified += OnSlotModifid;
        }

        private ItemSlot OnNewSlot(int id, InventoryGeneric inv) => new ItemSlotSurvival(inv)
        {
            MaxSlotStackSize = SlotStackSize
        };

        private void OnSlotModifid(int slot)
        {
            Api.World.BlockAccessor.GetChunkAtBlockPos(Pos)?.MarkModified();
        }

        private ItemSlot GetAutoPullFromSlot(BlockFacing atBlockFace) => null;

        private ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot)
        {
            if (atBlockFace == BlockFacing.UP)
            {
                return inventory[0];
            }
            return null;
        }

        private void ParseBlockProperties()
        {
            if (Block?.Attributes != null)
            {
                itemFlowRate = Block.Attributes["item-flowrate"].AsFloat(itemFlowRate);
                checkRateMs = Block.Attributes["item-checkrateMs"].AsInt(checkRateMs);
                inventoryClassName = Block.Attributes["inventoryClassName"].AsString(inventoryClassName);
                QuantitySlots = Block.Attributes["quantitySlots"].AsInt(QuantitySlots);
                SlotStackSize = Block.Attributes["slotStackSize"].AsInt(SlotStackSize);
            }
        }

        private float itemFlowRate = 0;
        private int checkRateMs = 200;
        private int QuantitySlots = 1;
        private int SlotStackSize = 1;

        public override void Initialize(ICoreAPI api)
        {
            InitInventory();
            base.Initialize(api);

            if (api is ICoreServerAPI)
            {
                RegisterDelayedCallback(dt => RegisterGameTickListener(ContinueFlow, checkRateMs), 10 + api.World.Rand.Next(200));
            }
        }

        private float itemFlowAccum;

        public void ContinueFlow(float timePassedInSeconds)
        {
            if (inventory.Empty) return;

            var firstItem = inventory.First(slot => !slot.Empty);

            itemFlowAccum = Math.Min(itemFlowAccum + itemFlowRate, Math.Max(1f, itemFlowRate * 2f));

            if (itemFlowAccum < 1) return;

            var outputPos = Pos.AddCopy(BlockFacing.DOWN);

            if (firstItem.Itemstack.Block != null && (!ChuteBlockPlacerModSystem.Config.UnstableFallingOnly || firstItem.Itemstack.Block.HasBehavior<BlockBehaviorUnstableFalling>()))
            {
                try
                {
                    if (ChuteBlockPlacerModSystem.Config.CreateEntityBlockFalling && ((Api.World as IServerWorldAccessor).Api as ICoreServerAPI).Server.Config.AllowFallingBlocks)
                    {
                        TryFall(firstItem, outputPos);
                    }
                    else
                    {
                        TryPlace(firstItem, outputPos);
                    }
                    return;
                }
                catch (Exception e)
                {
                    Api.Logger.Error($"TryPlace/TryFall failed due to the following exception: {e}");
                }
            }

            TrySpitOut(firstItem, outputPos);
        }

        private bool TryFall(ItemSlot slot, BlockPos pos)
        {
            var blockAtTarget = Api.World.BlockAccessor.GetBlock(pos);
            if (blockAtTarget.Replaceable <= 6000) return false;

            AssetLocation fallSound = null;
            float impactDamageMul = 1;
            float dustIntensity = 0;

            var unstableFallingBehaiour = slot.Itemstack.Block.GetBehavior<BlockBehaviorUnstableFalling>();
            if (unstableFallingBehaiour != null)
            {
                var beh = Traverse.Create(unstableFallingBehaiour);

                fallSound = beh.Field("fallSound").GetValue<AssetLocation>();
                impactDamageMul = beh.Field("impactDamageMul").GetValue<float>();
                dustIntensity = beh.Field("dustIntensity").GetValue<float>();
            }

            if (Api.World.GetNearestEntity(pos.ToVec3d().Add(0.5, 0.5, 0.5), 1f, 1.5f, (Entity e) => e is EntityBlockFalling entityBlockFalling && entityBlockFalling.initialPos.Equals(pos)) == null)
            {
                EntityBlockFalling entity = new(slot.Itemstack.Block, null, pos, fallSound, impactDamageMul, canFallSideways: true, dustIntensity);
                Api.World.SpawnEntity(entity);

                itemFlowAccum -= 1;
                slot.TakeOut(1);
                slot.MarkDirty();
                MarkDirty(false, null);
                return true;
            }
            return false;
        }

        private bool TryPlace(ItemSlot slot, BlockPos pos)
        {
            string failureCode = null;

            var placed = slot.Itemstack.Block.TryPlaceBlock(Api.World, null, slot.Itemstack, new BlockSelection
            {
                Position = pos,
                Face = BlockFacing.DOWN
            }, ref failureCode);

            if (placed)
            {
                itemFlowAccum -= 1;
                slot.TakeOut(1);
                slot.MarkDirty();
                MarkDirty(false, null);
            }
            return placed;
        }

        private bool TrySpitOut(ItemSlot slot, BlockPos pos)
        {
            if (Api.World.BlockAccessor.GetBlock(pos).Replaceable < 6000) return false;

            var outputFace = BlockFacing.DOWN;
            ItemStack stack = slot.TakeOut((int)itemFlowAccum);
            itemFlowAccum -= stack.StackSize;
            stack.Attributes.RemoveAttribute("chuteQHTravelled");
            stack.Attributes.RemoveAttribute("chuteDir");
            float velox = outputFace.Normalf.X / 10f + ((float)Api.World.Rand.NextDouble() / 20f - 0.05f) * Math.Sign(outputFace.Normalf.X);
            float veloy = outputFace.Normalf.Y / 10f + ((float)Api.World.Rand.NextDouble() / 20f - 0.05f) * Math.Sign(outputFace.Normalf.Y);
            float veloz = outputFace.Normalf.Z / 10f + ((float)Api.World.Rand.NextDouble() / 20f - 0.05f) * Math.Sign(outputFace.Normalf.Z);
            Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5 + (double)(outputFace.Normalf.X / 2f), 0.5 + (double)(outputFace.Normalf.Y / 2f), 0.5 + (double)(outputFace.Normalf.Z / 2f)), new Vec3d((double)velox, (double)veloy, (double)veloz));
            slot.MarkDirty();
            MarkDirty(false, null);

            return true;
        }

        private bool TrySpitOut(BlockFacing outputFace)
        {
            if (this.Api.World.BlockAccessor.GetBlock(this.Pos.AddCopy(outputFace)).Replaceable >= 6000)
            {
                ItemSlot itemSlot = this.inventory.FirstOrDefault((ItemSlot slot) => !slot.Empty);
                ItemStack stack = itemSlot.TakeOut((int)this.itemFlowAccum);
                this.itemFlowAccum -= (float)stack.StackSize;
                stack.Attributes.RemoveAttribute("chuteQHTravelled");
                stack.Attributes.RemoveAttribute("chuteDir");
                float velox = outputFace.Normalf.X / 10f + ((float)this.Api.World.Rand.NextDouble() / 20f - 0.05f) * (float)Math.Sign(outputFace.Normalf.X);
                float veloy = outputFace.Normalf.Y / 10f + ((float)this.Api.World.Rand.NextDouble() / 20f - 0.05f) * (float)Math.Sign(outputFace.Normalf.Y);
                float veloz = outputFace.Normalf.Z / 10f + ((float)this.Api.World.Rand.NextDouble() / 20f - 0.05f) * (float)Math.Sign(outputFace.Normalf.Z);
                this.Api.World.SpawnItemEntity(stack, this.Pos.ToVec3d().Add(0.5 + (double)(outputFace.Normalf.X / 2f), 0.5 + (double)(outputFace.Normalf.Y / 2f), 0.5 + (double)(outputFace.Normalf.Z / 2f)), new Vec3d((double)velox, (double)veloy, (double)veloz));
                itemSlot.MarkDirty();
                this.MarkDirty(false, null);
                return true;
            }
            return false;
        }

        public override void OnBlockRemoved()
        {
            if (Api is IServerWorldAccessor) DropContents();
            base.OnBlockRemoved();
        }

        public void DropContents()
        {
            Vec3d epos = Pos.ToVec3d().Add(0.5, 0.5, 0.5);
            foreach (ItemSlot slot in inventory)
            {
                if (slot.Itemstack != null)
                {
                    slot.Itemstack.Attributes.RemoveAttribute("chuteQHTravelled");
                    slot.Itemstack.Attributes.RemoveAttribute("chuteDir");
                    Api.World.SpawnItemEntity(slot.Itemstack, epos, null);
                    slot.Itemstack = null;
                    slot.MarkDirty();
                }
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            InitInventory();
            base.FromTreeAttributes(tree, worldForResolving);
        }

        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            return true;
        }
    }
}