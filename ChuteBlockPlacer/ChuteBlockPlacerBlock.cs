using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace ChuteBlockPlacer
{
    public class ChuteBlockPlacerBlock : Block, IBlockItemFlow
    {
        public bool HasItemFlowConnectorAt(BlockFacing facing) => facing == BlockFacing.UP;
    }
}