using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kanvas.Interface;
using System.Drawing;

namespace Kanvas.Swizzle
{
    public class CaféSwizzle : IImageSwizzle
    {
        public int Height { get; set; }
        public int Width { get; set; }
        MasterSwizzle swizzle;

        (int, int)[] coordsBlock4bpp = new[] { (1, 0), (2, 0), (0, 1), (0, 2), (4, 0), (0, 4), (8, 0), (16, 0), (0, 8), (0, 32), (32, 32), (64, 0), (0, 16) };
        (int, int)[] coordsBlock8bpp = new[] { (1, 0), (2, 0), (0, 1), (0, 2), (0, 4), (4, 0), (8, 0), (16, 0), (0, 32), (32, 32), (64, 0), (0, 8), (0, 16) };
        (int, int)[] coordsRegular8bpp = new[] { (1, 0), (2, 0), (4, 0), (0, 2), (0, 1), (0, 4), (32, 0), (64, 0), (0, 8), (8, 8), (16, 0) };
        (int, int)[] coordsRegular16bpp = new[] { (1, 0), (2, 0), (4, 0), (0, 1), (0, 2), (0, 4), (32, 0), (0, 8), (8, 8), (16, 0) };
        (int, int)[] coordsRegular32bpp = new[] { (1, 0), (2, 0), (0, 1), (4, 0), (0, 2), (0, 4), (0, 8), (8, 8), (16, 0) };

        public CaféSwizzle(byte swizzleTileMode, bool isBlockBased, int bitDepth, int width, int height)
        {
            if ((swizzleTileMode & 0x1F) < 2) throw new NotImplementedException();

            if ((swizzleTileMode & 0x1F) == 2 || (swizzleTileMode & 0x1F) == 3)
            {
                swizzle = new MasterSwizzle(width, new Point(0, 0), new[] { (0, 1), (0, 2), (1, 0), (2, 0), (4, 0), (0, 4) });
            }
            else
            {
                // Can be simplified further once we find more swizzles/formats. Left this way for now because it's easier to debug
                if (isBlockBased)
                {
                    var init = new[] { new Point(0, 0), new Point(32, 32), new Point(64, 0), new Point(96, 32) }[swizzleTileMode >> 6];
                    init.Y ^= swizzleTileMode & 32;

                    var coords = bitDepth == 4 ? coordsBlock4bpp : bitDepth == 8 ? coordsBlock8bpp : throw new Exception();
                    swizzle = new MasterSwizzle(width, init, coords, new[] { (64, 0), (32, 32) });
                }
                else
                {
                    var init = new[] { new Point(0, 0), new Point(8, 8), new Point(16, 0), new Point(24, 8) }[swizzleTileMode >> 6];
                    init.Y ^= (swizzleTileMode & 32) >> 2;

                    var coords = bitDepth == 8 ? coordsRegular8bpp : bitDepth == 16 ? coordsRegular16bpp : bitDepth == 32 ? coordsRegular32bpp : throw new Exception();
                    swizzle = new MasterSwizzle(width, init, coords, new[] { (16, 0), (8, 8) });
                }
            }
            Width = (width + swizzle.MacroTileWidth - 1) & -swizzle.MacroTileWidth;
            Height = (height + swizzle.MacroTileHeight - 1) & -swizzle.MacroTileHeight;
        }

        public Point Get(Point point) => swizzle.Get(point.Y * Width + point.X);
    }
}
