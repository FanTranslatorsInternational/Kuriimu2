using Kanvas.Contract;
using Kanvas.Contract.DataClasses;
using SixLabors.ImageSharp;

namespace Kanvas.Swizzle
{
    public class Ps4Swizzle : IImageSwizzle
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

        public Ps4Swizzle(SwizzleOptions context)
        {
            Width = (context.Size.Width + 3) & ~3;
            Height = (context.Size.Height + 3) & ~3;

            var isBlockEncoding = context.EncodingInfo.ColorsPerValue > 1;

            var bitField = new List<(int, int)>();
            var bitStart = isBlockEncoding ? 4 : 1;

            if (isBlockEncoding)
                bitField.AddRange([(1, 0), (2, 0), (0, 1), (0, 2)]);

            for (var i = bitStart; i < 32; i *= 2)
                bitField.AddRange([(i, 0), (0, i)]);

            _swizzle = new MasterSwizzle(Width, Point.Empty, [.. bitField]);
        }

        /// <inheritdoc />
        public Point Transform(Point point) => Get(point.Y * Width + point.X);

        /// <inheritdoc />
        public Point Get(int pointCount) => _swizzle.Get(pointCount);
    }
}
