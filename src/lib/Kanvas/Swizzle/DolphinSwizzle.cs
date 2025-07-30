using Kanvas.Contract;
using Kanvas.Contract.DataClasses;
using SixLabors.ImageSharp;

namespace Kanvas.Swizzle
{
    /// <summary>
    /// The swizzle used on the Nintendo GameCube.
    /// </summary>
    public class DolphinSwizzle : IImageSwizzle
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

        public DolphinSwizzle(SwizzleOptions context)
        {
            _swizzle = new MasterSwizzle(context.Size.Width, new Point(0, 0), [(1, 0), (2, 0), (0, 1), (0, 2)]);
            (Width, Height) = ((context.Size.Width + 3) & ~3, (context.Size.Height + 3) & ~3);
        }

        /// <inheritdoc />
        public Point Transform(Point point) => Get(point.Y * Width + point.X);

        /// <inheritdoc />
        public Point Get(int pointCount) => _swizzle.Get(pointCount);
    }
}
