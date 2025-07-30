using Kanvas.Contract;
using Kanvas.Contract.DataClasses;
using SixLabors.ImageSharp;

namespace Kanvas.Swizzle
{
    /// <summary>
    /// The swizzle used on the Nintendo WiiU.
    /// </summary>
    public class CafeSwizzle : IImageSwizzle
    {
        private const int RegularMaxSize_ = 128;

        // TODO: Coords for block based encodings are prepended by the preparation method
        private static readonly Dictionary<int, (int, int)[]> CoordsBlock = new()
        {
            [4] = [(1, 0), (2, 0), (0, 1), (0, 2), (4, 0), (0, 4), (8, 0), (16, 0), (0, 8), (0, 32), (32, 32), (64, 0), (0, 16)],
            [8] = [(1, 0), (2, 0), (0, 1), (0, 2), (0, 4), (4, 0), (8, 0), (16, 0), (0, 32), (32, 32), (64, 0), (0, 8), (0, 16)]
        };

        private static readonly Dictionary<int, (int, int)[]> CoordsRegular = new()
        {
            [08] = [(1, 0), (2, 0), (4, 0), (0, 2), (0, 1), (0, 4), (32, 0), (64, 0), (0, 8), (8, 8), (16, 0)],
            [16] = [(1, 0), (2, 0), (4, 0), (0, 1), (0, 2), (0, 4), (32, 0), (0, 8), (8, 8), (16, 0)],
            [32] = [(1, 0), (2, 0), (0, 1), (4, 0), (0, 2), (0, 4), (0, 8), (8, 8), (16, 0)],
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

        public CafeSwizzle(SwizzleOptions context, byte swizzleTileMode)
        {
            var isBlockBased = context.EncodingInfo.ColorsPerValue > 1;
            var bitDepth = context.EncodingInfo.BitDepth;

            if ((swizzleTileMode & 0x1F) < 2)
                throw new NotImplementedException();

            if ((swizzleTileMode & 0x1F) == 2 || (swizzleTileMode & 0x1F) == 3)
            {
                var bitField = new[] { (0, 1), (0, 2), (1, 0), (2, 0), (4, 0) };
                var bitFieldExtension = new List<(int, int)>();
                for (var i = 4; i < Math.Min(context.Size.Height, RegularMaxSize_); i *= 2)
                    bitFieldExtension.Add((0, i));

                _swizzle = new MasterSwizzle(context.Size.Width, new Point(0, 0), bitField.Concat(bitFieldExtension).ToArray());
            }
            else
            {
                // Can be simplified further once we find more swizzles/formats. Left this way for now because it's easier to debug
                if (isBlockBased)
                {
                    var init = new[] { new Point(0, 0), new Point(32, 32), new Point(64, 0), new Point(96, 32) }[swizzleTileMode >> 6];
                    init.Y ^= swizzleTileMode & 0x20;

                    _swizzle = new MasterSwizzle(context.Size.Width, init, CoordsBlock[bitDepth], [(64, 0), (32, 32)]);
                }
                else
                {
                    var init = new[] { new Point(0, 0), new Point(8, 8), new Point(16, 0), new Point(24, 8) }[swizzleTileMode >> 6];
                    init.Y ^= (swizzleTileMode & 0x20) >> 2;

                    _swizzle = new MasterSwizzle(context.Size.Width, init, CoordsRegular[bitDepth], [(16, 0), (8, 8)]);
                }
            }

            Width = (context.Size.Width + _swizzle.MacroTileWidth - 1) & -_swizzle.MacroTileWidth;
            Height = (context.Size.Height + _swizzle.MacroTileHeight - 1) & -_swizzle.MacroTileHeight;
        }

        /// <inheritdoc />
        public Point Transform(Point point) => Get(point.Y * Width + point.X);

        /// <inheritdoc />
        public Point Get(int pointCount) => _swizzle.Get(pointCount);
    }
}
