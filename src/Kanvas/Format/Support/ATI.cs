using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Kanvas.Support
{
    public class ATI
    {
        public enum Format
        {
            ATI1A, ATI1L, ATI2
        }

        public class Decoder
        {
            private Queue<Color> queue = new Queue<Color>();
            public Format Format { get; set; }

            private int Interpolate(int a, int b, int num, int den) => (num * a + (den - num) * b + den / 2) / den;

            public Decoder(Format format = Format.ATI1A)
            {
                Format = format;
            }

            public Color Get(Func<(ulong block, ulong block2)> func)
            {
                if (!queue.Any())
                {
                    var (block, block2) = func();

                    var p_0 = block & 0xFF;
                    var p_1 = block >> 8 & 0xFF;

                    if (Format == Format.ATI2)
                    {
                        var a_0 = block2 & 0xFF;
                        var a_1 = block2 >> 8 & 0xFF;

                        for (int i = 0; i < 16; i++)
                        {
                            var codeL = (int)(block >> 16 + 3 * i) & 7;
                            var codeA = (int)(block2 >> 16 + 3 * i) & 7;

                            var lum = (codeL == 0) ? (int)p_0
                            : codeL == 1 ? (int)p_1
                            : p_0 > p_1 ? Interpolate((int)p_0, (int)p_1, 8 - codeL, 7)
                            : codeL < 6 ? Interpolate((int)p_0, (int)p_1, 6 - codeL, 5)
                            : (codeL - 6) * 255;

                            var alpha = (codeA == 0) ? (int)a_0
                            : codeA == 1 ? (int)a_1
                            : a_0 > a_1 ? Interpolate((int)a_0, (int)a_1, 8 - codeA, 7)
                            : codeA < 6 ? Interpolate((int)a_0, (int)a_1, 6 - codeA, 5)
                            : (codeA - 6) * 255;

                            queue.Enqueue(Color.FromArgb(alpha, lum, lum, lum));
                        }
                    }
                    else
                    {
                        for (int i = 0; i < 16; i++)
                        {
                            var code = (int)(block >> 16 + 3 * i) & 7;
                            var value = (code == 0) ? (int)p_0
                                : code == 1 ? (int)p_1
                                : p_0 > p_1 ? Interpolate((int)p_0, (int)p_1, 8 - code, 7)
                                : code < 6 ? Interpolate((int)p_0, (int)p_1, 6 - code, 5)
                                : (code - 6) * 255;

                            queue.Enqueue((Format == Format.ATI1A) ?
                                Color.FromArgb(value, 255, 255, 255) :
                                Color.FromArgb(255, value, value, value));
                        }
                    }
                }
                return queue.Dequeue();
            }
        }

        public class Encoder
        {
            List<Color> queue = new List<Color>();
            public Format Format { get; set; }

            public Encoder(Format format = Format.ATI1A)
            {
                Format = format;
            }

            public void Set(Color c, Action<(ulong alpha, ulong block)> func)
            {
                queue.Add(c);
                if (queue.Count == 16)
                {
                    // Alpha
                    if (Format == Format.ATI1A)
                    {
                        ulong outAlpha = 0;
                        var alphaEncoder = new BCn.BC4BlockEncoder();
                        alphaEncoder.LoadBlock(queue.Select(clr => clr.A / 255f).ToArray());
                        outAlpha = alphaEncoder.EncodeUnsigned().PackedValue;

                        func((0, outAlpha));
                    }
                    //Luminance
                    else if (Format == Format.ATI1L)
                    {
                        ulong outLum = 0;
                        var lumEncoder = new BCn.BC4BlockEncoder();
                        lumEncoder.LoadBlock(queue.Select(clr => clr.R / 255f).ToArray());
                        outLum = lumEncoder.EncodeUnsigned().PackedValue;

                        func((0, outLum));
                    }
                    //ATI2 - Both
                    else if (Format == Format.ATI2)
                    {
                        var alphaEncoder = new BCn.BC4BlockEncoder();
                        alphaEncoder.LoadBlock(queue.Select(clr => clr.A / 255f).ToArray());
                        var outAlpha = alphaEncoder.EncodeUnsigned().PackedValue;

                        var lumEncoder = new BCn.BC4BlockEncoder();
                        lumEncoder.LoadBlock(queue.Select(clr => clr.R / 255f).ToArray());
                        var outLum = lumEncoder.EncodeUnsigned().PackedValue;

                        func((outAlpha, outLum));
                    }

                    queue.Clear();
                }
            }
        }

        /*public class Encoder
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
        }*/
    }
}