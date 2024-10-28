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
        public bool UnstableFallingOnly { get; set; } = true;
    }
}