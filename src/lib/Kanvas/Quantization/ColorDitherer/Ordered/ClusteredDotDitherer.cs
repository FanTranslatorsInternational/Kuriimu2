using SixLabors.ImageSharp;

namespace Kanvas.Quantization.ColorDitherer.Ordered
{
    public class ClusteredDotDitherer(Size imageSize, int taskCount) : OrderedDitherer(imageSize, taskCount)
    {
        protected override byte[,] Matrix => new byte[,]
        {
            { 13,  5, 12, 16 },
            {  6,  0,  4, 11 },
            {  7,  2,  3, 10 },
            { 14,  8,  9, 15 }
        };
    }
}
