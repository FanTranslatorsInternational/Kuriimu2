using SixLabors.ImageSharp;

namespace Kanvas.Quantization.ColorDitherer.ErrorDiffusion
{
    public class ShiauFan2Ditherer : ErrorDiffusionDitherer
    {
        protected override byte[,] Matrix => new byte[,]
        {
            { 0, 0, 0, 0, 0, 0, 0},
            { 0, 0, 0, 0, 8, 0, 0},
            { 1, 1, 2, 4, 0, 0, 0}
        };

        protected override int MatrixSideWidth => 3;
        protected override int MatrixSideHeight => 1;
        protected override int ErrorLimit => 16;

        public ShiauFan2Ditherer(Size imageSize, int taskCount) :
            base(imageSize, taskCount)
        {
        }
    }
}
