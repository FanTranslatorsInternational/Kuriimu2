using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Kanvas.Support
{
    public class DXT
    {
        public enum Formats
        {
            DXT1, DXT3, DXT5
        }

        public class Decoder
        {
            private Queue<Color> queue = new Queue<Color>();
            public Formats Format { get; set; }

            private int Interpolate(int a, int b, int num, int den) => (num * a + (den - num) * b + den / 2) / den;
            private Color InterpolateColor(Color a, Color b, int num, int den) => Color.FromArgb(Interpolate(a.R, b.R, num, den), Interpolate(a.G, b.G, num, den), Interpolate(a.B, b.B, num, den));

            private Color GetRGB565(ushort val) => Color.FromArgb(255, (val >> 11) * 33 / 4, (val >> 5) % 64 * 65 / 16, (val % 32) * 33 / 4);

            public Decoder(Formats format = Formats.DXT1)
            {
                Format = format;
            }

            public Color Get(Func<(ulong alpha, ulong block)> func)
            {
                if (!queue.Any())
                {
                    var (alpha, block) = func();

                    var (a0, a1) = ((byte)alpha, (byte)(alpha >> 8));
                    var (color0, color1) = ((ushort)block, (ushort)(block >> 16));
                    var (c0, c1) = (GetRGB565(color0), GetRGB565(color1));

                    for (int i = 0; i < 16; i++)
                    {
                        var code = (int)(alpha >> 16 + 3 * i) & 7;
                        var alp = 255;
                        if (Format == Formats.DXT3)
                        {
                            // TODO: Fix broken DXT3 alpha Neobeo!
                            // The DXT3 images might be using yet another color mess thing, maybe
                            alp = ((int)(alpha >> (4 * i)) & 0xF) * 17;
                        }
                        else if (Format == Formats.DXT5)
                        {
                            alp = code == 0 ? a0
                            : code == 1 ? a1
                            : a0 > a1 ? Interpolate(a0, a1, 8 - code, 7)
                            : code < 6 ? Interpolate(a0, a1, 6 - code, 5)
                            : code % 2 * 255;
                        }
                        code = (int)(block >> 32 + 2 * i) & 3;
                        Color clr = code == 0 ? c0
                                : code == 1 ? c1
                                : Format == Formats.DXT5 || color0 > color1 ? InterpolateColor(c0, c1, 4 - code, 3)
                                : code == 2 ? InterpolateColor(c0, c1, 1, 2)
                                : Color.Black;
                        queue.Enqueue(Color.FromArgb(alp, clr));
                    }
                }
                return queue.Dequeue();
            }
        }

        public class Encoder
        {
            List<Color> queue = new List<Color>();
            public Formats Format { get; set; }

            public Encoder(Formats format = Formats.DXT1)
            {
                Format = format;
            }

            public void Set(Color c, Action<(ulong alpha, ulong block)> func)
            {
                queue.Add(c);
                if (queue.Count == 16)
                {
                    // Alpha
                    ulong outAlpha = 0;
                    if (Format == Formats.DXT5)
                    {
                        var alphaEncoder = new BCn.BC4BlockEncoder();
                        alphaEncoder.LoadBlock(queue.Select(clr => clr.A / 255f).ToArray());
                        outAlpha = alphaEncoder.EncodeUnsigned().PackedValue;
                    }

                    // Color
                    var colorEncoder = new BCn.BC1BlockEncoder();
                    colorEncoder.LoadBlock(
                        queue.Select(clr => clr.R / 255f).ToArray(),
                        queue.Select(clr => clr.G / 255f).ToArray(),
                        queue.Select(clr => clr.B / 255f).ToArray()
                    );
                    var outColor = colorEncoder.Encode().PackedValue;

                    func((outAlpha, outColor));
                    queue.Clear();
                }
            }
        }
    }
}