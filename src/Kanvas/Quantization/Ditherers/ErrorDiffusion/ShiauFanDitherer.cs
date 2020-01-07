using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Quantization.Ditherers.ErrorDiffusion
{
    public class ShiauFanDitherer : BaseErrorDiffusionDitherer
    {
        protected override byte[,] Matrix => new byte[,]
        {
            { 0, 0, 0, 0, 0},
            { 0, 0, 0, 4, 0},
            { 1, 1, 2, 0, 0}
        };

        protected override int MatrixSideWidth => 2;
        protected override int MatrixSideHeight => 1;
        protected override int ErrorLimit => 8;

        public ShiauFanDitherer(int width, int height, int taskCount) :
            base(width, height, taskCount)
        {
        }
    }
}
