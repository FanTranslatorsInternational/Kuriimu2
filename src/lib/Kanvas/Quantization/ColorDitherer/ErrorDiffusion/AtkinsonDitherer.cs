using SixLabors.ImageSharp;

namespace Kanvas.Quantization.ColorDitherer.ErrorDiffusion
{
    public class AtkinsonDitherer : ErrorDiffusionDitherer
    {
        protected override byte[,] Matrix => new byte[,]
        {
            { 0, 0, 0, 0, 0},
            { 0, 0, 0, 0, 0},
            { 0, 0, 0, 1, 1},
            { 0, 1, 1, 1, 0},
            { 0, 0, 1, 0, 0}
        };

        protected override int MatrixSideWidth => 2;
        protected override int MatrixSideHeight => 2;
        protected override int ErrorLimit => 8;

        public AtkinsonDitherer(Size imageSize, int taskCount) :
            base(imageSize, taskCount)
        {
        }
    }
}
