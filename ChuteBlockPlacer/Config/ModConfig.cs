using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChuteBlockPlacer.Config
{
    public class ModConfig
    {
        /// <summary>
        /// Whether this mod can place any block or only blocks like sand (and soil if enabled)
        /// </summary>
        public bool UnstableFallingOnly { get; set; } = false;

        /// <summary>
        /// Wether blocks a EntityBlockFalling should be created instead of just placing the block (this will allow even normal blocks to fall down from the hopper)
        /// </summary>
        public bool CreateEntityBlockFalling { get; set; } = true;
    }
}