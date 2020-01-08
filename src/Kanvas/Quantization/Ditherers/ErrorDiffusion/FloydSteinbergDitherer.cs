using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Quantization.Ditherers.ErrorDiffusion
{
    public class FloydSteinbergDitherer : BaseErrorDiffusionDitherer
    {
        protected override byte[,] Matrix => new byte[,]
        {
            { 0, 0, 0},
            { 0, 0, 7},
            { 3, 5, 1}
        };

        protected override int MatrixSideWidth => 1;
        protected override int MatrixSideHeight => 1;
        protected override int ErrorLimit => 16;

        public FloydSteinbergDitherer(Size imageSize, int taskCount) :
            base(imageSize, taskCount)
        {
        }
    }
}
