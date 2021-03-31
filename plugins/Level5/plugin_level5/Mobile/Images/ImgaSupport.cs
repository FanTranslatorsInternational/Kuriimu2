using System.Collections.Generic;
using System.Drawing;
using Kanvas;
using Kanvas.Swizzle;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Kanvas.Model;
using Kontract.Models.Image;

namespace plugin_level5.Mobile.Images
{
    public class ImgaHeader
    {
        [FixedLength(4)]
        public string magic; // IMGA
        public int const1; // 30 30 00 00
        public short const2; // 30 00
        public byte imageFormat;
        public byte const3; // 01
        public byte imageCount;
        public byte bitDepth;
        public short bytesPerTile;

        public short width;
        public short height;
        public int const4; // 30 00 00 00
        public int const5; // 30 00 01 00
        public int tableDataOffset; // always 0x48

        public int const6; // 00 00 00 00
        public int const7; // 00 00 00 00
        public int const8; // 00 00 00 00
        public int const9; // 00 00 00 00

        public int const10; // 00 00 00 00
        public int tileTableSize;
        public int tileTableSizePadded;
        public int imgDataSize;

        public int const11; // 00 00 00 00
        public int const12; // 00 00 00 00
    }

    class ImgaSwizzle : IImageSwizzle
    {
        private readonly MasterSwizzle _zOrder;

        public int Width { get; }
        public int Height { get; }

        public ImgaSwizzle(SwizzlePreparationContext context)
        {
            Width = (context.Size.Width + 0x7) & ~0x7;
            Height = (context.Size.Height + 0x7) & ~0x7;

            _zOrder = new MasterSwizzle(Width, Point.Empty, new[] { (1, 0), (2, 0), (4, 0), (0, 1), (0, 2), (0, 4) });
        }

        public Point Transform(Point point)
        {
            return _zOrder.Get(point.Y * Width + point.X);
        }
    }

    class ImgaSupport
    {
        private static IDictionary<int, IColorEncoding> Formats = new Dictionary<int, IColorEncoding>
        {
            [0x03] = ImageFormats.Rgb888()
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(Formats);

            return definition;
        }
    }
}
