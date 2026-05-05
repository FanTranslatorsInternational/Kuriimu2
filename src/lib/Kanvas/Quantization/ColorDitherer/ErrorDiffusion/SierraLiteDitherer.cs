using SixLabors.ImageSharp;

namespace Kanvas.Quantization.ColorDitherer.ErrorDiffusion
{
    public class SierraLiteDitherer(Size imageSize, int taskCount) : ErrorDiffusionDitherer(imageSize, taskCount)
    {
        protected override byte[,] Matrix => new byte[,]
        {
            { 0, 0, 0},
            { 0, 0, 2},
            { 1, 1, 0}
        };

        protected override int MatrixSideWidth => 1;
        protected override int MatrixSideHeight => 1;
        protected override int ErrorLimit => 4;
    }
}
