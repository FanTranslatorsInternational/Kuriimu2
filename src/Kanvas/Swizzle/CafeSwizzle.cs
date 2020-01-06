using System;
using System.Drawing;
using Kontract.Kanvas;

namespace Kanvas.Swizzle
{
    /// <summary>
    /// The swizzle used on the Nintendo WiiU.
    /// </summary>
    public class CafeSwizzle : IImageSwizzle
    {
        private readonly MasterSwizzle _swizzle;

        private readonly (int, int)[] _coordsBlock4Bpp = { (1, 0), (2, 0), (0, 1), (0, 2), (4, 0), (0, 4), (8, 0), (16, 0), (0, 8), (0, 32), (32, 32), (64, 0), (0, 16) };
        private readonly (int, int)[] _coordsBlock8Bpp = { (1, 0), (2, 0), (0, 1), (0, 2), (0, 4), (4, 0), (8, 0), (16, 0), (0, 32), (32, 32), (64, 0), (0, 8), (0, 16) };
        private readonly (int, int)[] _coordsRegular8Bpp = { (1, 0), (2, 0), (4, 0), (0, 2), (0, 1), (0, 4), (32, 0), (64, 0), (0, 8), (8, 8), (16, 0) };
        private readonly (int, int)[] _coordsRegular16Bpp = { (1, 0), (2, 0), (4, 0), (0, 1), (0, 2), (0, 4), (32, 0), (0, 8), (8, 8), (16, 0) };
        private readonly (int, int)[] _coordsRegular32Bpp = { (1, 0), (2, 0), (0, 1), (4, 0), (0, 2), (0, 4), (0, 8), (8, 8), (16, 0) };

        /// <inheritdoc />
        public int Width { get;  }

        /// <inheritdoc />
        public int Height { get;  }

        /// <summary>
        /// Creates a new instance of <see cref="CafeSwizzle"/>.
        /// </summary>
        /// <param name="swizzleTileMode">The tile mode of the swizzle</param>
        /// <param name="isBlockBased">Is the swizzle block based.</param>
        /// <param name="bitDepth">Bit depth of the components.</param>
        /// <param name="width">The width of the image to swizzle.</param>
        /// <param name="height">The height of the image to swizzle.</param>
        public CafeSwizzle(byte swizzleTileMode, bool isBlockBased, int bitDepth, int width, int height)
        {
            if ((swizzleTileMode & 0x1F) < 2)
                throw new NotImplementedException();

            if ((swizzleTileMode & 0x1F) == 2 || (swizzleTileMode & 0x1F) == 3)
            {
                _swizzle = new MasterSwizzle(width, new Point(0, 0), new[] { (0, 1), (0, 2), (1, 0), (2, 0), (4, 0), (0, 4) });
            }
            else
            {
                // Can be simplified further once we find more swizzles/formats. Left this way for now because it's easier to debug
                if (isBlockBased)
                {
                    var init = new[] { new Point(0, 0), new Point(32, 32), new Point(64, 0), new Point(96, 32) }[swizzleTileMode >> 6];
                    init.Y ^= swizzleTileMode & 32;

                    var coords = bitDepth == 4 ? _coordsBlock4Bpp : bitDepth == 8 ? _coordsBlock8Bpp : throw new Exception();
                    _swizzle = new MasterSwizzle(width, init, coords, new[] { (64, 0), (32, 32) });
                }
                else
                {
                    var init = new[] { new Point(0, 0), new Point(8, 8), new Point(16, 0), new Point(24, 8) }[swizzleTileMode >> 6];
                    init.Y ^= (swizzleTileMode & 32) >> 2;

                    var coords = bitDepth == 8 ? _coordsRegular8Bpp : bitDepth == 16 ? _coordsRegular16Bpp : bitDepth == 32 ? _coordsRegular32Bpp : throw new Exception();
                    _swizzle = new MasterSwizzle(width, init, coords, new[] { (16, 0), (8, 8) });
                }
            }

            Width = (width + _swizzle.MacroTileWidth - 1) & -_swizzle.MacroTileWidth;
            Height = (height + _swizzle.MacroTileHeight - 1) & -_swizzle.MacroTileHeight;
        }

        /// <inheritdoc />
        public Point Transform(Point point) => _swizzle.Get(point.Y * Width + point.X);
    }
}
