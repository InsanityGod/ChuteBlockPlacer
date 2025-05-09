﻿using ChuteBlockPlacer.Code.BlockEntities;
using ChuteBlockPlacer.Code.Blocks;
using ChuteBlockPlacer.Config;
using System;
using Vintagestory.API.Common;

namespace ChuteBlockPlacer.Code
{
    public class ChuteBlockPlacerModSystem : ModSystem
    {
        private const string ConfigName = "ChuteBlockPlacerConfig.json";

        public static ModConfig Config { get; private set; }

        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockClass("ChuteBlockPlacerBlock", typeof(ChuteBlockPlacerBlock));
            api.RegisterBlockEntityClass("PlacerItemFlow", typeof(BlockEntityPlacerItemFlow));

            LoadConfig(api);
        }
        //TODO EntityFalling.UpdateBlock
        private static void LoadConfig(ICoreAPI api)
        {
            try
            {
                Config ??= api.LoadModConfig<ModConfig>(ConfigName);
                if (Config == null)
                {
                    Config = new();
                    api.StoreModConfig(Config, ConfigName);
                }
            }
            catch (Exception ex)
            {
                api.Logger.Error(ex);
                api.Logger.Warning("Failed to load config, using default values instead");
                Config = new();
            }
        }

        public override void Dispose()
        {
            Config = null;
        }
    }
}