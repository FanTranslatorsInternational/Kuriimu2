﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Quantization.Ditherers.ErrorDiffusion
{
    public class ShiauFan2Ditherer : BaseErrorDiffusionDitherer
    {
        protected override byte[,] Matrix => new byte[,]
        {
            { 0, 0, 0, 0, 0, 0, 0},
            { 0, 0, 0, 0, 8, 0, 0},
            { 1, 1, 2, 4, 0, 0, 0}
        };

        protected override int MatrixSideWidth => 3;
        protected override int MatrixSideHeight => 1;
        protected override int ErrorLimit => 16;

        public ShiauFan2Ditherer(Size imageSize, int taskCount) :
            base(imageSize, taskCount)
        {
        }
    }
}
