using SixLabors.ImageSharp;

namespace Kanvas.Quantization.ColorDitherer.ErrorDiffusion
{
    public class FloydSteinbergDitherer : ErrorDiffusionDitherer
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
