using System;
using System.Collections.Generic;
using System.Drawing;
using Kanvas.Encoding.Models;
using Kontract.Kanvas;

namespace Kanvas.Encoding.BlockCompressions.BCn
{
    class BC1BlockDecoder
    {
        private static readonly IPixelDescriptor Rgb565 =
            new RgbaPixelDescriptor("RGB", 5, 6, 5, 0);

        private static readonly Lazy<BC1BlockDecoder> Lazy = new Lazy<BC1BlockDecoder>(() => new BC1BlockDecoder());
        public static BC1BlockDecoder Instance => Lazy.Value;

        public IEnumerable<Color> Process(ulong data)
        {
            // If color0 > color1, we can store 1 bit alpha
            // Otherwise no alpha

            var (color0, color1) = ((ushort)data, (ushort)(data >> 16));
            var (c0, c1) = (Rgb565.GetColor(color0), Rgb565.GetColor(color1));

            for (var i = 0; i < 16; i++)
            {
                var code = (int)(data >> (32 + 2 * i)) & 3;

                if (color0 > color1)
                {
                    yield return code == 0 ? c0 :
                        code == 1 ? c1 :
                        code == 2 ? InterpolateColor(c0, c1, 2, 3) :
                        Color.Transparent;
                }
                else
                {
                    yield return code == 0 ? c0 :
                        code == 1 ? c1 :
                        InterpolateColor(c0, c1, 4 - code, 3);
                }
            }
        }

        private Color InterpolateColor(Color a, Color b, int num, float den) => Color.FromArgb(
            Interpolate(a.R, b.R, num, den),
            Interpolate(a.G, b.G, num, den),
            Interpolate(a.B, b.B, num, den));

        private int Interpolate(int a, int b, int num, float den) =>
            (int)((num * a + (den - num) * b) / den);
    }
}
