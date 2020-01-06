using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Quantization.Ditherers.ErrorDiffusion
{
    public class Sierra3RowDitherer : BaseErrorDiffusionDitherer
    {
        protected override byte[,] Matrix => new byte[,]
        {
            { 0, 0, 0, 0, 0},
            { 0, 0, 0, 0, 0},
            { 0, 0, 0, 5, 3},
            { 2, 4, 5, 4, 2},
            { 0, 2, 3, 2, 0}
        };

        protected override int MatrixSideWidth => 2;
        protected override int MatrixSideHeight => 2;
        protected override int ErrorLimit => 32;

        public Sierra3RowDitherer(int width, int height) :
            base(width, height)
        {
        }
    }
}
