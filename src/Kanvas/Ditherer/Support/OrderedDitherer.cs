using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Kanvas.Ditherer.Support
{
    public static class OrderedDitherer
    {
        public static IEnumerable<Color> TransformColors(IEnumerable<Color> source, List<Color> palette, int MatrixWidth, int MatrixHeight, int ImageWidth, int[,] CachedMatrix)
        {
            var target = new List<Color>(source);

            int index = 0;
            foreach (var p in source)
            {
                Point srcP = new Point(index % ImageWidth, index / ImageWidth);

                // retrieves matrix coordinates
                int x = srcP.X % MatrixWidth;
                int y = srcP.Y % MatrixHeight;

                // reads the source pixel
                Color oldColor = p;

                // converts alpha to solid color
                oldColor = Color.FromArgb(255, oldColor.R, oldColor.G, oldColor.B);

                // determines the threshold
                var threshold = CachedMatrix[x, y] + 1;

                // only process dithering if threshold is substantial
                if (threshold > 0)
                {
                    int red = GetClampedColorElement(oldColor.R + threshold);
                    int green = GetClampedColorElement(oldColor.G + threshold);
                    int blue = GetClampedColorElement(oldColor.B + threshold);

                    Color newColor = Color.FromArgb(255, red, green, blue);

                    target[index] = palette[NearestColor(palette, newColor)];
                }

                index++;
            }

            return target;
        }

        // closed match in RGB space
        static int NearestColor(List<Color> colors, Color target)
        {
            var colorDiffs = colors.Select(n => ColorDiff(n, target)).Min(n => n);
            return colors.FindIndex(n => ColorDiff(n, target) == colorDiffs);
        }

        // distance in RGB space
        static int ColorDiff(Color c1, Color c2)
        {
            return (int)Math.Sqrt((c1.R - c2.R) * (c1.R - c2.R)
                                   + (c1.G - c2.G) * (c1.G - c2.G)
                                   + (c1.B - c2.B) * (c1.B - c2.B));
        }

        static int GetClampedColorElement(int colorElement)
        {
            int result = colorElement;
            if (result < 0) result = 0;
            if (result > 255) result = 255;
            return result;
        }
    }
}
