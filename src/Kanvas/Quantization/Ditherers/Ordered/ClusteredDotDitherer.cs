using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Quantization.Ditherers.Ordered
{
    public class ClusteredDotDitherer : BaseOrderDitherer
    {
        protected override byte[,] Matrix => new byte[,]
        {
            { 13,  5, 12, 16 },
            {  6,  0,  4, 11 },
            {  7,  2,  3, 10 },
            { 14,  8,  9, 15 }
        };
    }
}
