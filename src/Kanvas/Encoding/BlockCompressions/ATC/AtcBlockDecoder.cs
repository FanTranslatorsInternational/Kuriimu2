using System;
using System.Collections.Generic;
using System.Drawing;
using Kanvas.Encoding.Models;
using Kontract.Kanvas;

namespace Kanvas.Encoding.BlockCompressions.ATC
{
    class AtcBlockDecoder
    {
        private static readonly IPixelDescriptor Rgb565 =
            new RgbaPixelDescriptor("RGB", 5, 6, 5, 0);
        private static readonly IPixelDescriptor Rgb555 =
            new RgbaPixelDescriptor("RGB", 5, 5, 5, 0);

        private static readonly Lazy<AtcBlockDecoder> Lazy = new Lazy<AtcBlockDecoder>(() => new AtcBlockDecoder());
        public static AtcBlockDecoder Instance => Lazy.Value;

        public IEnumerable<Color> Process(ulong data)
        {
            var (color0, color1) = ((ushort)data, (ushort)(data >> 16));
            var (c0, c1) = (Rgb555.GetColor(color0), Rgb565.GetColor(color1));
            var method = color0 >> 15;

            for (var i = 0; i < 16; i++)
            {
                var code = (int)(data >> (32 + 2 * i)) & 3;

                if (method == 0)
                {
                    yield return InterpolateColor0(c0, c1, code, 3);
                }
                else
                {
                    yield return code == 0 ? Color.Black :
                        code == 1 ? InterpolateColor1(c0, c1, 1, 4) :
                        code == 2 ? c0 :
                        c1;
                }
            }
        }

        private Color InterpolateColor0(Color a, Color b, int num, float den) => Color.FromArgb(
            Interpolate0(a.R, b.R, num, den),
            Interpolate0(a.G, b.G, num, den),
            Interpolate0(a.B, b.B, num, den));

        private int Interpolate0(int a, int b, int num, float den) =>
            (int)(num / den * (b - a) + a);

        private Color InterpolateColor1(Color a, Color b, int num, float den) => Color.FromArgb(
            Interpolate1(a.R, b.R, num, den),
            Interpolate1(a.G, b.G, num, den),
            Interpolate1(a.B, b.B, num, den));

        private int Interpolate1(int a, int b, int num, float den) =>
            (int)(a - num / den * b);
    }
}
