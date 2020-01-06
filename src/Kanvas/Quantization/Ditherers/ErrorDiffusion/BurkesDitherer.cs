using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Quantization.Ditherers.ErrorDiffusion
{
    public class BurkesDitherer : BaseErrorDiffusionDitherer
    {
        protected override byte[,] Matrix => new byte[,]
        {
            { 0, 0, 0, 0, 0},
            { 0, 0, 0, 8, 4},
            { 2, 4, 8, 4, 2}
        };

        protected override int MatrixSideWidth => 2;
        protected override int MatrixSideHeight => 1;
        protected override int ErrorLimit => 32;

        public BurkesDitherer(int width, int height) :
            base(width, height)
        {
        }
    }
}
