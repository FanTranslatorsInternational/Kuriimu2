using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Kanvas.Format.Support
{
    public class ATC
    {
        public enum AlphaMode : int
        {
            Explicit,
            Interpolated
        }

        public class Decoder
        {
            private Queue<Color> queue = new Queue<Color>();

            private Color GetRGB565(ushort val) => Color.FromArgb(255, (val >> 11) * 33 / 4, (val >> 5) % 64 * 65 / 16, (val % 32) * 33 / 4);
            private Color GetRGB555(ushort val) => Color.FromArgb(255, ((val >> 10) % 32) * 33 / 4, ((val >> 5) % 32) * 33 / 4, (val % 32) * 33 / 4);

            private int Clamp(int value) => Math.Min(255, Math.Max(0, value));

            private Color InterpolateColor(Color c0, Color c1, double code) => Color.FromArgb(255, Clamp((int)(c0.R + (code / 3 * (c1.R - c0.R)))), Clamp((int)(c0.G + (code / 3 * (c1.G - c0.G)))), Clamp((int)(c0.B + (code / 3 * (c1.B - c0.B)))));
            private Color InterpolateColorDiv(Color c0, Color c1, double div) => Color.FromArgb(255, Clamp((int)(c0.R - (1 / div * c1.R))), Clamp((int)(c0.G - (1 / div * c1.G))), Clamp((int)(c0.B - (1 / div * c1.B))));

            private int Interpolate(int a, int b, int num, int den) => (num * a + (den - num) * b + den / 2) / den;

            bool alpha;
            AlphaMode alphaMode;

            public Decoder(bool alpha, AlphaMode alphaMode)
            {
                this.alpha = alpha;
                this.alphaMode = alphaMode;
            }

            public Color Get(Func<(ulong alpha, ulong block)> func)
            {
                if (!queue.Any())
                {
                    var (alphaBlock, block) = func();

                    var (a0, a1) = ((byte)alphaBlock, (byte)(alphaBlock >> 8));
                    var (color0, color1) = ((ushort)block, (ushort)(block >> 16));
                    var (c0, c1) = (GetRGB555(color0), GetRGB565(color1));
                    var method = color0 >> 15;

                    for (int i = 0; i < 16; i++)
                    {
                        var alphaValue = 255;
                        if (alpha)
                            if (alphaMode == AlphaMode.Explicit)
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

                        queue.Enqueue(Color.FromArgb(alphaValue, colorValue));
                    }
                }
                return queue.Dequeue();
            }
        }

        private static ushort From565To555(ushort value) => (ushort)(value & 0x1F | (Helper.ChangeBitDepth((value >> 5) & 0x3F, 6, 5) << 5) | (((value >> 11) & 0x1F) << 10));
        private static Dictionary<int, ulong> Remap = new Dictionary<int, ulong>
        {
            [0] = 0,
            [1] = 3,
            [2] = 1,
            [3] = 2
        };

        public class Encoder
        {
            List<Color> queue = new List<Color>();
            bool alpha;
            AlphaMode alphaMode;

            public Encoder(bool alpha, AlphaMode alphaMode)
            {
                this.alpha = alpha;
                this.alphaMode = alphaMode;
            }

            public void Set(Color c, Action<(ulong alpha, ulong block)> func)
            {
                queue.Add(c);
                if (queue.Count == 16)
                {
                    // Alpha
                    ulong outAlpha = 0;
                    if (alphaMode == AlphaMode.Interpolated)
                    {
                        var alphaEncoder = new BCn.BC4BlockEncoder();
                        alphaEncoder.LoadBlock(queue.Select(clr => clr.A / 255f).ToArray());
                        outAlpha = alphaEncoder.EncodeUnsigned().PackedValue;
                    }
                    else
                    {
                        var alphaEncoder = new BCn.BC2ABlockEncoder();
                        alphaEncoder.LoadBlock(queue.Select(clr => clr.A / 255f).ToArray());
                        outAlpha = alphaEncoder.Encode().PackedValue;
                    }

                    // Color
                    var colorEncoder = new BCn.BC1BlockEncoder();
                    colorEncoder.LoadBlock(
                        queue.Select(clr => clr.R / 255f).ToArray(),
                        queue.Select(clr => clr.G / 255f).ToArray(),
                        queue.Select(clr => clr.B / 255f).ToArray()
                    );
                    var outColor = colorEncoder.Encode().PackedValue;

                    //ATC specific modifications to BC1
                    //according to http://www.guildsoftware.com/papers/2012.Converting.DXTC.to.ATC.pdf
                    //change color0 from rgb565 to rgb555 with method 0
                    outColor = (outColor & ~0xFFFFUL) | (From565To555((ushort)outColor));

                    //change color codes
                    for (int i = 0; i < 16; i++)
                        outColor = (outColor & ~((ulong)0x3 << (32 + i * 2))) | Remap[(int)((outColor >> (32 + 2 * i)) & 0x3)] << (32 + i * 2);

                    func((outAlpha, outColor));
                    queue.Clear();
                }
            }
        }
    }
}