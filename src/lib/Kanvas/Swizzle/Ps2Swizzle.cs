using System;
using System.Collections.Generic;
using System.Drawing;
using Kontract.Kanvas.Interfaces;
using Kontract.Kanvas.Models;

namespace Kanvas.Swizzle
{
    /* https://ps2linux.no-ip.info/playstation2-linux.com/docs/howto/display_docef7c.html?docid=75 */
    public class Ps2Swizzle : IImageSwizzle
    {
        private readonly MasterSwizzle _swizzle;

        public int Width { get; }
        public int Height { get; }

        public Ps2Swizzle(SwizzlePreparationContext context)
        {
            Width = 2 << (int)Math.Log(context.Size.Width - 1, 2);
            Height = (context.Size.Height + 7) & ~7;

            switch (context.EncodingInfo.BitDepth)
            {
                case 4:
                case 16:
                    throw new InvalidOperationException($"Unsupported PS2 swizzle for bit depth {context.EncodingInfo.BitDepth}");

                case 8:
                    var seq = new List<(int, int)> { (4, 2), (8, 0), (1, 0), (2, 0), (4, 0) };
                    for (var i = 16; i < Width; i *= 2) 
                        seq.Add((i, 0));
                    seq.AddRange(new[] { (0, 1), (4, 4) });

                    _swizzle = new MasterSwizzle(context.Size.Width, Point.Empty, seq.ToArray());
                    break;
            }
        }

        public Point Transform(Point point) => _swizzle.Get(point.Y * Width + point.X);
    }
}
