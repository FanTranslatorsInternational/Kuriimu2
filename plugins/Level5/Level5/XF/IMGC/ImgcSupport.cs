using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Drawing;
using Komponent.IO;
using Kanvas.Interface;
using Kanvas.Format;
using Kanvas.Swizzle;
using Komponent.IO.Attributes;

namespace Kore.XFont.Image
{
    public class IMGCHeader
    {
        [FixedLength(4)]
        public string magic; // IMGC
        public int const1; // 30 30 00 00
        public short const2; // 30 00
        public byte imageFormat;
        public byte const3; // 01
        public byte combineFormat;
        public byte bitDepth;
        public short bytesPerTile;
        public short width;
        public short height;
        public int const4; // 30 00 00 00
        public int const5; // 30 00 01 00
        public int tableDataOffset; // always 0x48
        public int const6; // 03 00 00 00
        public int const7; // 00 00 00 00
        public int const8; // 00 00 00 00
        public int const9; // 00 00 00 00
        public int const10; // 00 00 00 00
        public int tableSize1;
        public int tableSize2;
        public int imgDataSize;
        public int const11; // 00 00 00 00
        public int const12; // 00 00 00 00
    }

    public class Support
    {
        public static Dictionary<byte, IImageFormat> Format = new Dictionary<byte, IImageFormat>
        {
            [0] = new RGBA(8, 8, 8, 8),
            [1] = new RGBA(4, 4, 4, 4),
            [2] = new RGBA(5, 5, 5, 1),
            [3] = new RGBA(8, 8, 8),
            [4] = new RGBA(5, 6, 5),
            [11] = new LA(8, 8),
            [12] = new LA(4, 4),
            [13] = new LA(8, 0),
            [14] = new HL(8, 8),
            [15] = new LA(0, 8),
            [26] = new LA(4, 0),
            //[27] = new LA(0, 4),
            //[27] = new ETC1(),
            //[28] = new ETC1(),
            //[29] = new ETC1(true)
        };
    }

    public class ImgcSwizzle : IImageSwizzle
    {
        MasterSwizzle _zorderTrans;

        public int Width { get; }
        public int Height { get; }

        public ImgcSwizzle(int width, int height)
        {
            Width = (width + 0x7) & ~0x7;
            Height = (height + 0x7) & ~0x7;

            _zorderTrans = new MasterSwizzle(Width, new Point(0, 0), new[] { (0, 1), (1, 0), (0, 2), (2, 0), (0, 4), (4, 0) });
        }

        public Point Get(Point point)
        {
            return _zorderTrans.Get(point.Y * Width + point.X);
        }
    }
}
