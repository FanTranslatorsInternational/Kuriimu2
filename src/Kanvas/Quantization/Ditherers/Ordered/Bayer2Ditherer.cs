using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Quantization.Ditherers.Ordered
{
    public class Bayer2Ditherer : BaseOrderDitherer
    {
        protected override byte[,] Matrix => new byte[,]
        {
            {1, 3},
            {4, 2}
        };

        public Bayer2Ditherer(Size imageSize, int taskCount) :
            base(imageSize, taskCount)
        {
        }
    }
}
