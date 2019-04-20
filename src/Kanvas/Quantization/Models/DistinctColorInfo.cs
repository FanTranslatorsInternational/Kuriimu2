using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Quantization.Models
{
    class DistinctColorInfo
    {
        private const int Factor = 5000000;

        public int Count { get; private set; }

        public int Color { get; }

        public int Hue { get; }

        public int Saturation { get; }

        public int Brightness { get; }

        public DistinctColorInfo(Color color)
        {
            Color = color.ToArgb();

            if ((uint)color.ToArgb() == 0xff2b675d)
                ;

            var value = System.Drawing.Color.FromArgb(0xff,0x2b,0x67,0x5d).GetHue();
            var res = System.Runtime.CompilerServices.Unsafe.As<float, int>(ref value);

            Hue = Convert.ToInt32(GetHue(color) * Factor);
            Saturation = Convert.ToInt32(GetSaturation(color) * Factor);
            Brightness = Convert.ToInt32(GetBrightness(color)* Factor);

            Count = 1;
        }

        public DistinctColorInfo IncreaseCount()
        {
            Count++;
            return this;
        }

        private double GetHue(Color color)
        {
            if (color.R == color.G && color.G == color.B)
                return 0; // 0 makes as good an UNDEFINED value as any

            double r = (double)color.R / 255.0d;
            double g = (double)color.G / 255.0d;
            double b = (double)color.B / 255.0d;

            double max, min;
            double delta;
            double hue = 0.0f;

            max = r; min = r;

            if (g > max) max = g;
            if (b > max) max = b;

            if (g < min) min = g;
            if (b < min) min = b;

            delta = max - min;

            if (r == max)
            {
                hue = (g - b) / delta;
            }
            else if (g == max)
            {
                hue = 2 + (b - r) / delta;
            }
            else if (b == max)
            {
                hue = 4 + (r - g) / delta;
            }
            hue *= 60;

            if (hue < 0.0f)
            {
                hue += 360.0f;
            }
            return hue;
        }

        public double GetSaturation(Color color)
        {
            double r = (double)color.R / 255.0d;
            double g = (double)color.G / 255.0d;
            double b = (double)color.B / 255.0d;

            double max, min;
            double l, s = 0;

            max = r; min = r;

            if (g > max) max = g;
            if (b > max) max = b;

            if (g < min) min = g;
            if (b < min) min = b;

            // if max == min, then there is no color and
            // the saturation is zero.
            //
            if (max != min)
            {
                l = (max + min) / 2;

                if (l <= .5)
                {
                    s = (max - min) / (max + min);
                }
                else
                {
                    s = (max - min) / (2 - max - min);
                }
            }
            return s;
        }

        public double GetBrightness(Color color)
        {
            double r = (double)color.R / 255.0d;
            double g = (double)color.G / 255.0d;
            double b = (double)color.B / 255.0d;

            double max, min;

            max = r; min = r;

            if (g > max) max = g;
            if (b > max) max = b;

            if (g < min) min = g;
            if (b < min) min = b;

            return (max + min) / 2;
        }
    }
}
