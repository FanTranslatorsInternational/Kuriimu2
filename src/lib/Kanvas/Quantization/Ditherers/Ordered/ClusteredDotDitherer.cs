﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Quantization.Ditherers.Ordered
{
    public class ClusteredDotDitherer : BaseOrderedDitherer
    {
        protected override byte[,] Matrix => new byte[,]
        {
            { 13,  5, 12, 16 },
            {  6,  0,  4, 11 },
            {  7,  2,  3, 10 },
            { 14,  8,  9, 15 }
        };

        public ClusteredDotDitherer(Size imageSize, int taskCount) :
            base(imageSize, taskCount)
        {
        }
    }
}
