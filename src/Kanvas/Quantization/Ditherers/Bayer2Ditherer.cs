using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Quantization.Ditherers
{
    public class Bayer2Ditherer : BaseOrderDitherer
    {
        protected override byte[,] Matrix => new byte[,]
        {
            {1, 3},
            {4, 2}
        };
    }
}
