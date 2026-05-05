using SixLabors.ImageSharp;

namespace Kanvas.Quantization.ColorDitherer.ErrorDiffusion
{
    public class Sierra2RowDitherer(Size imageSize, int taskCount) : ErrorDiffusionDitherer(imageSize, taskCount)
    {
        protected override byte[,] Matrix => new byte[,]
        {
            { 0, 0, 0, 0, 0},
            { 0, 0, 0, 4, 3},
            { 1, 2, 3, 2, 1}
        };

        protected override int MatrixSideWidth => 2;
        protected override int MatrixSideHeight => 1;
        protected override int ErrorLimit => 16;
    }
}
