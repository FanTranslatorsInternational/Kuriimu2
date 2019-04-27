using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas.Encoding.Support.ATC.Models;

namespace Kanvas.Encoding.Support.ATC
{
    internal class Decoder
    {
        private readonly Queue<Color> _queue;

        private readonly AlphaMode _alphaMode;

        private Color GetRGB565(ushort val) => Color.FromArgb(255, (val >> 11) * 33 / 4, (val >> 5) % 64 * 65 / 16, (val % 32) * 33 / 4);
        private Color GetRGB555(ushort val) => Color.FromArgb(255, ((val >> 10) % 32) * 33 / 4, ((val >> 5) % 32) * 33 / 4, (val % 32) * 33 / 4);

        private int Clamp(int value) => Math.Min(255, Math.Max(0, value));

        private Color InterpolateColor(Color c0, Color c1, double code) => Color.FromArgb(255, Clamp((int)(c0.R + (code / 3 * (c1.R - c0.R)))), Clamp((int)(c0.G + (code / 3 * (c1.G - c0.G)))), Clamp((int)(c0.B + (code / 3 * (c1.B - c0.B)))));
        private Color InterpolateColorDiv(Color c0, Color c1, double div) => Color.FromArgb(255, Clamp((int)(c0.R - (1 / div * c1.R))), Clamp((int)(c0.G - (1 / div * c1.G))), Clamp((int)(c0.B - (1 / div * c1.B))));

        private int Interpolate(int a, int b, int num, int den) => (num * a + (den - num) * b + den / 2) / den;

        public Decoder(AlphaMode alphaMode)
        {
            _queue = new Queue<Color>();
            _alphaMode = alphaMode;
        }

        public Color Get(Func<(ulong alpha, ulong block)> func)
        {
            if (_queue.Any())
                return _queue.Dequeue();

            var (alphaBlock, block) = func();

            var (a0, a1) = ((byte)alphaBlock, (byte)(alphaBlock >> 8));
            var (color0, color1) = ((ushort)block, (ushort)(block >> 16));
            var (c0, c1) = (GetRGB555(color0), GetRGB565(color1));
            var method = color0 >> 15;

            for (int i = 0; i < 16; i++)
            {
                var alphaValue = 255;
                if (_alphaMode != AlphaMode.None)
                    if (_alphaMode == AlphaMode.Explicit)
                        alphaValue = (int)((alphaBlock >> (i * 4)) & 0xF) * 17;
                    else
                    {
                        var alphaCode = (int)(alphaBlock >> 16 + 3 * i) & 7;
                        alphaValue = alphaCode == 0 ? a0
                            : alphaCode == 1 ? a1
                            : a0 > a1 ? Interpolate(a0, a1, 8 - alphaCode, 7)
                            : alphaCode < 6 ? Interpolate(a0, a1, 6 - alphaCode, 5)
                            : alphaCode % 2 * 255;
                    }

                Color colorValue;
                var code = (int)(block >> 32 + 2 * i) & 0x3;
                if (method == 0)
                {
                    colorValue = InterpolateColor(c0, c1, code);
                }
                else
                {
                    colorValue = (code == 0) ? Color.Black
                        : (code == 1) ? InterpolateColorDiv(c0, c1, 4)
                        : (code == 2) ? c0
                        : c1;
                }

                _queue.Enqueue(Color.FromArgb(alphaValue, colorValue));
            }
            return _queue.Dequeue();
        }
    }
}
