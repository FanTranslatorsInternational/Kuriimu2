using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas.Encoding.BlockCompressions.DXT.Models;

namespace Kanvas.Encoding.BlockCompressions.DXT
{
    internal class Decoder
    {
        private readonly Queue<Color> _queue;
        private readonly DxtFormat _format;

        private int Interpolate(int a, int b, int num, int den) => (num * a + (den - num) * b + den / 2) / den;
        private Color InterpolateColor(Color a, Color b, int num, int den) => Color.FromArgb(Interpolate(a.R, b.R, num, den), Interpolate(a.G, b.G, num, den), Interpolate(a.B, b.B, num, den));

        private Color GetRGB565(ushort val) => Color.FromArgb(255, (val >> 11) * 33 / 4, (val >> 5) % 64 * 65 / 16, (val % 32) * 33 / 4);

        public Decoder(DxtFormat format)
        {
            _queue = new Queue<Color>();
            _format = format;
        }

        public Color Get(Func<(ulong alpha, ulong block)> func)
        {
            if (_queue.Any())
                return _queue.Dequeue();

            var (alpha, block) = func();

            var (a0, a1) = ((byte)alpha, (byte)(alpha >> 8));
            var (color0, color1) = ((ushort)block, (ushort)(block >> 16));
            var (c0, c1) = (GetRGB565(color0), GetRGB565(color1));

            for (int i = 0; i < 16; i++)
            {
                var code = (int)(alpha >> 16 + 3 * i) & 7;
                var alp = 255;
                if (_format == DxtFormat.DXT3)
                {
                    // Fix broken DXT3 alpha Neobeo?
                    // The DXT3 images might be using yet another color mess thing, maybe
                    alp = ((int)(alpha >> (4 * i)) & 0xF) * 17;
                }
                else if (_format == DxtFormat.DXT5)
                {
                    alp = code == 0 ? a0
                        : code == 1 ? a1
                        : a0 > a1 ? Interpolate(a0, a1, 8 - code, 7)
                        : code < 6 ? Interpolate(a0, a1, 6 - code, 5)
                        : code % 2 * 255;
                }
                code = (int)(block >> 32 + 2 * i) & 3;
                var clr = code == 0 ? c0
                    : code == 1 ? c1
                    : _format == DxtFormat.DXT5 || color0 > color1 ? InterpolateColor(c0, c1, 4 - code, 3)
                    : code == 2 ? InterpolateColor(c0, c1, 1, 2)
                    : Color.Black;
                _queue.Enqueue(Color.FromArgb(alp, clr));
            }

            return _queue.Dequeue();
        }
    }
}
