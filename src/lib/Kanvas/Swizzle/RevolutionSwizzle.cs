using Kanvas.Contract;
using Kanvas.Contract.DataClasses;
using SixLabors.ImageSharp;

namespace Kanvas.Swizzle
{
    /// <summary>
    /// The swizzle for the NintendoW Wii, code named Revolution.
    /// </summary>
    public class RevolutionSwizzle : IImageSwizzle
    {
        private readonly IDictionary<int, (int, int)[]> _bitFields = new Dictionary<int, (int, int)[]>
        {
            [04] = [(1, 0), (2, 0), (4, 0), (0, 1), (0, 2), (0, 4)],
            [08] = [(1, 0), (2, 0), (4, 0), (0, 1), (0, 2)],
            [16] = [(1, 0), (2, 0), (0, 1), (0, 2)],
            [32] = [(1, 0), (2, 0), (0, 1), (0, 2)]
        };

        private readonly MasterSwizzle _swizzle;

        /// <inheritdoc />
        public int Width { get; }

        /// <inheritdoc />
        public int Height { get; }

        /// <inheritdoc />
        public int MacroTileWidth => _swizzle.MacroTileWidth;

        /// <inheritdoc />
        public int MacroTileHeight => _swizzle.MacroTileHeight;

        public RevolutionSwizzle(SwizzleOptions context)
        {
            var widthStride = _bitFields[context.EncodingInfo.BitDepth].Sum(x => x.Item1);
            var heightStride = _bitFields[context.EncodingInfo.BitDepth].Sum(x => x.Item2);

            Width = (context.Size.Width + widthStride) & ~widthStride;
            Height = (context.Size.Height + heightStride) & ~heightStride;

            _swizzle = new MasterSwizzle(Width, Point.Empty, _bitFields[context.EncodingInfo.BitDepth]);
        }

        /// <inheritdoc />
        public Point Transform(Point point) => Get(point.Y * Width + point.X);

        /// <inheritdoc />
        public Point Get(int pointCount) => _swizzle.Get(pointCount);
    }
}
