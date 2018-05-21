using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Kanvas.Interface;

//Closest color:
//https://stackoverflow.com/questions/27374550/how-to-compare-color-object-and-get-closest-color-in-an-color?utm_medium=organic&utm_source=google_rich_qa&utm_campaign=google_rich_qa

namespace Kanvas.Support
{
    public class Helper
    {
        public static int ChangeBitDepth(int value, int bitDepthFrom, int bitDepthTo)
        {
            if (bitDepthTo < 0 || bitDepthFrom < 0)
                throw new Exception("BitDepths can't be negative!");
            if (bitDepthFrom == 0 || bitDepthTo == 0)
                return 0;
            if (bitDepthFrom == bitDepthTo)
                return value;

            if (bitDepthFrom < bitDepthTo)
            {
                var fromMaxRange = (1 << bitDepthFrom) - 1;
                var toMaxRange = (1 << bitDepthTo) - 1;

                var div = 1;
                while (toMaxRange % fromMaxRange != 0)
                {
                    div <<= 1;
                    toMaxRange = ((toMaxRange + 1) << 1) - 1;
                }

                return value * (toMaxRange / fromMaxRange) / div;
            }
            else
            {
                var fromMax = 1 << bitDepthFrom;
                var toMax = 1 << bitDepthTo;

                var limit = fromMax / toMax;

                return value / limit;
            }
        }

        public static Color GetClosesColor(List<Color> colors, Color target, ColorDistance colorDistance)
        {
            switch (colorDistance)
            {
                case ColorDistance.OnlyHUE:
                    var hue1 = target.GetHue();
                    var diffs = colors.Select(n => getHueDistance(n.GetHue(), hue1));
                    var diffMin = diffs.Min(n => n);
                    return colors[diffs.ToList().FindIndex(n => n == diffMin)];
                case ColorDistance.DirectDistance:
                    var colorDiffs = colors.Select(n => ColorDiff(n, target)).Min(n => n);
                    return colors.Find(n => ColorDiff(n, target) == colorDiffs);
                case ColorDistance.HSVWeighting:
                    float hue2 = target.GetHue();
                    var num1 = ColorNum(target, 1, 1);
                    var diffs2 = colors.Select(n => Math.Abs(ColorNum(n, 1, 1) - num1) +
                                                   getHueDistance(n.GetHue(), hue2));
                    var diffMin2 = diffs2.Min(x => x);
                    return colors[diffs2.ToList().FindIndex(n => n == diffMin2)];
                default:
                    return Color.Magenta;
            }
        }

        // color brightness as perceived:
        private static float getBrightness(Color c)
        { return (c.R * 0.299f + c.G * 0.587f + c.B * 0.114f) / 256f; }

        // distance between two hues:
        private static float getHueDistance(float hue1, float hue2)
        {
            float d = Math.Abs(hue1 - hue2); return d > 180 ? 360 - d : d;
        }

        //  weighed only by saturation and brightness
        private static float ColorNum(Color c, float factorSat, float factorBri)
        {
            return c.GetSaturation() * factorSat +
                        getBrightness(c) * factorBri;
        }

        // distance in RGB space
        private static int ColorDiff(Color c1, Color c2)
        {
            return (int)Math.Sqrt((c1.R - c2.R) * (c1.R - c2.R)
                                   + (c1.G - c2.G) * (c1.G - c2.G)
                                   + (c1.B - c2.B) * (c1.B - c2.B));
        }
    }
}
