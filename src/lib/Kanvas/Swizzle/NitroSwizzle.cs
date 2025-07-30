using Kanvas.Contract;
using Kanvas.Contract.DataClasses;
using SixLabors.ImageSharp;

namespace Kanvas.Swizzle
{
    /// <summary>
    /// The swizzle used on the Nintendo DS.
    /// </summary>
    public class NitroSwizzle : IImageSwizzle
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

        public NitroSwizzle(SwizzleOptions context)
        {
            _swizzle = new MasterSwizzle(context.Size.Width, new Point(0, 0), [(1, 0), (2, 0), (4, 0), (0, 1), (0, 2), (0, 4)]);
            (Width, Height) = ((context.Size.Width + 7) & ~7, (context.Size.Height + 7) & ~7);
        }

        /// <inheritdoc />
        public Point Transform(Point point) => Get(point.Y * Width + point.X);

        /// <inheritdoc />
        public Point Get(int pointCount) => _swizzle.Get(pointCount);
    }
}
