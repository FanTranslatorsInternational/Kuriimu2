using SixLabors.ImageSharp;

namespace Kanvas.Quantization.ColorDitherer.ErrorDiffusion
{
    public class FloydSteinbergDitherer(Size imageSize, int taskCount) : ErrorDiffusionDitherer(imageSize, taskCount)
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
    }
}
