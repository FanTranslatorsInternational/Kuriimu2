using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Quantization.Ditherers.ErrorDiffusion
{
    public class ZhigangFanDitherer : BaseErrorDiffusionDitherer
    {
        protected override byte[,] Matrix => new byte[,]
        {
            { 0, 0, 0, 0, 0},
            { 0, 0, 0, 7, 0},
            { 1, 3, 5, 0, 0}
        };

        protected override int MatrixSideWidth => 2;
        protected override int MatrixSideHeight => 1;
        protected override int ErrorLimit => 16;

        public ZhigangFanDitherer(int width, int height) :
            base(width, height)
        {
        }
    }
}
