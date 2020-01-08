using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kanvas.Quantization.Models;

namespace Kanvas.Quantization.Ditherers.Ordered
{
    public class Bayer4Ditherer : BaseOrderDitherer
    {
        protected override byte[,] Matrix => new byte[,]
        {
            {1, 9, 3, 11},
            {13, 5, 15, 7},
            {4, 12, 2, 10},
            {16, 8, 14, 6}
        };

        public Bayer4Ditherer(Size imageSize, int taskCount) :
            base(imageSize, taskCount)
        {
        }
    }
}
