using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace ChuteBlockPlacer.Code.Blocks
{
    public class ChuteBlockPlacerBlock : Block, IBlockItemFlow
    {
        public string[] PullFaces
        {
            get
            {
                return Attributes["pullFaces"].AsArray(Array.Empty<string>(), null);
            }
        }

        public BlockFacing Facing { get; private set; }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            Facing = BlockFacing.FromCode(Variant["orientation"]);
        }

        public bool HasItemFlowConnectorAt(BlockFacing facing) => PullFaces.Contains(facing.Code);

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            var orientation = blockSel.Face.IsVertical ? "down" : blockSel.Face.Code;

            if (world.GetBlock(new AssetLocation(CodeWithVariant("orientation", orientation))) is not ChuteBlockPlacerBlock block) return false;

            world.BlockAccessor.SetBlock(block.BlockId, blockSel.Position);
            return true;
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            var block = world.GetBlock(new AssetLocation(CodeWithVariant("orientation", "down")));
            return new ItemStack[]
            {
                new(block, 1)
            };
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos) => GetDrops(world, pos, null, 1f)[0];
    }
}