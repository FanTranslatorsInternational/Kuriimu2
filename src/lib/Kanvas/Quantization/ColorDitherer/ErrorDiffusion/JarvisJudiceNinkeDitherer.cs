using SixLabors.ImageSharp;

namespace Kanvas.Quantization.ColorDitherer.ErrorDiffusion
{
    public class JarvisJudiceNinkeDitherer(Size imageSize, int taskCount) : ErrorDiffusionDitherer(imageSize, taskCount)
    {
        protected override byte[,] Matrix => new byte[,]
        {
            { 0, 0, 0, 0, 0},
            { 0, 0, 0, 0, 0},
            { 0, 0, 0, 7, 5},
            { 3, 5, 7, 5, 3},
            { 1, 3, 5, 3, 1}
        };

        protected override int MatrixSideWidth => 2;
        protected override int MatrixSideHeight => 2;
        protected override int ErrorLimit => 48;
    }
}
