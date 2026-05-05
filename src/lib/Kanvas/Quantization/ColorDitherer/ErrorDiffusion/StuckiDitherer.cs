using SixLabors.ImageSharp;

namespace Kanvas.Quantization.ColorDitherer.ErrorDiffusion
{
    public class StuckiDitherer(Size imageSize, int taskCount) : ErrorDiffusionDitherer(imageSize, taskCount)
    {
        protected override byte[,] Matrix => new byte[,]
        {
            { 0, 0, 0, 0, 0},
            { 0, 0, 0, 0, 0},
            { 0, 0, 0, 8, 4},
            { 2, 4, 8, 4, 2},
            { 1, 2, 4, 2, 1}
        };

        protected override int MatrixSideWidth => 2;
        protected override int MatrixSideHeight => 2;
        protected override int ErrorLimit => 42;
    }
}
