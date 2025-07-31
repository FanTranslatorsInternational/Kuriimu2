using Kanvas.Contract;
using Kanvas.Contract.DataClasses;
using SixLabors.ImageSharp;

namespace Kanvas.Swizzle
{
    /* https://ps2linux.no-ip.info/playstation2-linux.com/docs/howto/display_docef7c.html?docid=75 */
    public class Ps2Swizzle : IImageSwizzle
    {
        private readonly MasterSwizzle _swizzle;

        /// <inheritdoc />
        public int Width { get; }

        /// <inheritdoc />
        public int Height { get; }

        /// <inheritdoc />
        public int MacroTileWidth => _swizzle.MacroTileWidth;

        /// <inheritdoc />
        public int MacroTileHeight => _swizzle.MacroTileHeight;

        public Ps2Swizzle(SwizzleOptions context)
        {
            Width = 2 << (int)Math.Log(context.Size.Width - 1, 2);
            Height = (context.Size.Height + 7) & ~7;

            switch (context.EncodingInfo.BitDepth)
            {
                case 8:
                    var seq = new List<(int, int)> { (4, 2), (8, 0), (1, 0), (2, 0), (4, 0) };
                    for (var i = 16; i < Width; i *= 2) 
                        seq.Add((i, 0));
                    seq.AddRange([(0, 1), (4, 4)]);

                    _swizzle = new MasterSwizzle(context.Size.Width, Point.Empty, seq.ToArray());
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported PS2 swizzle for bit depth {context.EncodingInfo.BitDepth}");
            }
        }

        /// <inheritdoc />
        public Point Transform(Point point) => Get(point.Y * Width + point.X);

        /// <inheritdoc />
        public Point Get(int pointCount) => _swizzle.Get(pointCount);
    }
}
