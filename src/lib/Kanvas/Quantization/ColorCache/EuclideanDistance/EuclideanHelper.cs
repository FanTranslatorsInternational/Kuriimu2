using SixLabors.ImageSharp.PixelFormats;

namespace Kanvas.Quantization.ColorCache.EuclideanDistance
{
    internal class EuclideanHelper
    {
        public static int GetSmallestEuclideanDistanceIndex(IList<Rgba32> palette, Rgba32 sourceColor)
        {
            return palette.Select((targetColor, index) => (targetColor, index))
                .MinBy(x => GetEuclideanDistance(sourceColor, x.targetColor))
                .index;
        }

        public static long GetEuclideanDistance(Rgba32 color)
        {
            var (r, g, b, a) = (color.R, color.G, color.B, color.A);
            return r * r + g * g + b * b + a * a;
        }

        private static long GetEuclideanDistance(Rgba32 sourceColor, Rgba32 targetColor)
        {
            var (rd, gd, bd, ad) = GetDifference(sourceColor, targetColor);
            return rd * rd + gd * gd + bd * bd + ad * ad;
        }

        private static (int rd, int gd, int bd, int ad) GetDifference(Rgba32 sourceColor, Rgba32 targetColor)
        {
            return (sourceColor.R - targetColor.R, sourceColor.G - targetColor.G, sourceColor.B - targetColor.B, sourceColor.A - targetColor.A);
        }
    }
}
