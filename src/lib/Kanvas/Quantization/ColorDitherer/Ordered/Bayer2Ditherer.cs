using SixLabors.ImageSharp;

namespace Kanvas.Quantization.ColorDitherer.Ordered
{
    public class Bayer2Ditherer(Size imageSize, int taskCount) : OrderedDitherer(imageSize, taskCount)
    {
        protected override byte[,] Matrix => new byte[,]
        {
            {1, 3},
            {4, 2}
        };
    }
}
