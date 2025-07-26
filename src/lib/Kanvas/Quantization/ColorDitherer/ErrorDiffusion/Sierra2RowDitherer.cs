using SixLabors.ImageSharp;

namespace Kanvas.Quantization.ColorDitherer.ErrorDiffusion
{
    public class Sierra2RowDitherer : ErrorDiffusionDitherer
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

        public Sierra2RowDitherer(Size imageSize, int taskCount) :
            base(imageSize, taskCount)
        {
        }
    }
}
