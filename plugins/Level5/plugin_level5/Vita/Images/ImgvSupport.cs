using System.Collections.Generic;
using System.Drawing;
using Kanvas;
using Kanvas.Encoding;
using Kanvas.Swizzle;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Kanvas.Model;
using Kontract.Models.Image;
#pragma warning disable 649

namespace plugin_level5.Vita.Images
{
    class ImgvHeader
    {
        [FixedLength(4)]
        public string magic; // IMGV
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
        public int const6; // 03 00 00 00
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

    class ImgvSwizzle : IImageSwizzle
    {
        private readonly MasterSwizzle _swizzle;

        public int Width { get; }
        public int Height { get; }

        public ImgvSwizzle(SwizzlePreparationContext context)
        {
            Width = (context.Size.Width + 0x7) & ~0x7;
            Height = (context.Size.Height + 0x7) & ~0x7;

            if (context.EncodingInfo.FormatName.ToLower().Contains("dxt"))
                _swizzle = new MasterSwizzle(Width, Point.Empty, new[] { (1, 0), (2, 0), (0, 1), (0, 2), (4, 0), (8, 0) });
            else
                _swizzle = new MasterSwizzle(Width, Point.Empty, new[] { (1, 0), (2, 0), (4, 0), (0, 1), (0, 2), (0, 4) });
        }

        public Point Transform(Point point)
        {
            return _swizzle.Get(point.Y * Width + point.X);
        }
    }

    class ImgvSupport
    {
        public static Dictionary<int, IColorEncoding> ImgvFormats = new Dictionary<int, IColorEncoding>
        {
            [0x03] = ImageFormats.Rgb888(),

            [0x1E] = ImageFormats.Dxt1()
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(ImgvFormats);

            return definition;
        }
    }
}
