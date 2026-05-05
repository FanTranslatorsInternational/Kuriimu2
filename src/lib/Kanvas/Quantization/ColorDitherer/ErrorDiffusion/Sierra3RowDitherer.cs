using SixLabors.ImageSharp;

namespace Kanvas.Quantization.ColorDitherer.ErrorDiffusion
{
    public class Sierra3RowDitherer(Size imageSize, int taskCount) : ErrorDiffusionDitherer(imageSize, taskCount)
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
    }
}
