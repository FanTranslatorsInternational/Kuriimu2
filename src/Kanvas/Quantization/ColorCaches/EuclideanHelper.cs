using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas.MoreEnumerable;

namespace Kanvas.Quantization.ColorCaches
{
    class EuclideanHelper
    {
        public static int GetSmallestEuclideanDistanceIndex(IList<Color> palette, Color sourceColor)
        {
            return palette.Select((targetColor, index) => (targetColor, index))
                .MinBy(x => GetEuclideanDistance(sourceColor, x.targetColor))
                .index;
        }

        public static long GetEuclideanDistance(Color color)
        {
            var (r, g, b, a) = (color.R, color.G, color.B, color.A);
            return r * r + g * g + b * b + a * a;
        }

        private static long GetEuclideanDistance(Color sourceColor, Color targetColor)
        {
            var (rd, gd, bd, ad) = GetDifference(sourceColor, targetColor);
            return rd * rd + gd * gd + bd * bd + ad * ad;
        }

        private static (int rd, int gd, int bd, int ad) GetDifference(Color sourceColor, Color targetColor)
        {
            return (sourceColor.R - targetColor.R, sourceColor.G - targetColor.G, sourceColor.B - targetColor.B, sourceColor.A - targetColor.A);
        }
    }
}
