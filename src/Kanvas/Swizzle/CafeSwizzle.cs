using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kontract.Kanvas;
using Kontract.Kanvas.Model;

namespace Kanvas.Swizzle
{
    /// <summary>
    /// The swizzle used on the Nintendo WiiU.
    /// </summary>
    public class CafeSwizzle : IImageSwizzle
    {
        private const int RegularMaxSize_ = 128;

        // TODO: Coords for block based encodings are prepended by the preparation method
        private static readonly Dictionary<int, (int, int)[]> CoordsBlock = new Dictionary<int, (int, int)[]>
        {
            [4] = new[] { (1, 0), (2, 0), (0, 1), (0, 2), (4, 0), (0, 4), (8, 0), (16, 0), (0, 8), (0, 32), (32, 32), (64, 0), (0, 16) },
            [8] = new[] { (1, 0), (2, 0), (0, 1), (0, 2), (0, 4), (4, 0), (8, 0), (16, 0), (0, 32), (32, 32), (64, 0), (0, 8), (0, 16) }
        };

        private static readonly Dictionary<int, (int, int)[]> CoordsRegular = new Dictionary<int, (int, int)[]>
        {
            [08] = new[] { (1, 0), (2, 0), (4, 0), (0, 2), (0, 1), (0, 4), (32, 0), (64, 0), (0, 8), (8, 8), (16, 0) },
            [16] = new[] { (1, 0), (2, 0), (4, 0), (0, 1), (0, 2), (0, 4), (32, 0), (0, 8), (8, 8), (16, 0) },
            [32] = new[] { (1, 0), (2, 0), (0, 1), (4, 0), (0, 2), (0, 4), (0, 8), (8, 8), (16, 0) },
        };

        private readonly MasterSwizzle _swizzle;

        public int Width { get; }
        public int Height { get; }

        public CafeSwizzle(SwizzlePreparationContext context, byte swizzleTileMode)
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

                    _swizzle = new MasterSwizzle(context.Size.Width, init, CoordsBlock[bitDepth], new[] { (64, 0), (32, 32) });
                }
                else
                {
                    var init = new[] { new Point(0, 0), new Point(8, 8), new Point(16, 0), new Point(24, 8) }[swizzleTileMode >> 6];
                    init.Y ^= (swizzleTileMode & 0x20) >> 2;

                    _swizzle = new MasterSwizzle(context.Size.Width, init, CoordsRegular[bitDepth], new[] { (16, 0), (8, 8) });
                }
            }

            Width = (context.Size.Width + _swizzle.MacroTileWidth - 1) & -_swizzle.MacroTileWidth;
            Height = (context.Size.Height + _swizzle.MacroTileHeight - 1) & -_swizzle.MacroTileHeight;
        }

        /// <inheritdoc />
        public Point Transform(Point point) => _swizzle.Get(point.Y * Width + point.X);
    }
}
