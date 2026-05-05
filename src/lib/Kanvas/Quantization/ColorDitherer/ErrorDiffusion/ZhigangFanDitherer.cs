using SixLabors.ImageSharp;

namespace Kanvas.Quantization.ColorDitherer.ErrorDiffusion
{
    public class ZhigangFanDitherer(Size imageSize, int taskCount) : ErrorDiffusionDitherer(imageSize, taskCount)
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
    }
}
