using Kanvas;
using Kanvas.Encoding;
using Kanvas.Swizzle;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Kanvas.Model;
using Kontract.Models.Image;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace plugin_dotemu.Images
{
    class DatConstants
    {
        public int paletteOffset = 0x200;
        public int dataOffset = 0x400;
    }
    class DatHeader
    {
        public short width;
        public short height;
    }
    class DatSwizzle : IImageSwizzle
    {
        MasterSwizzle _swizzle;
        public DatSwizzle(SwizzlePreparationContext context)
        {
            Width = (context.Size.Width + 3) & ~3;
            Height = (context.Size.Height + 3) & ~3;

            _swizzle = new MasterSwizzle(Width, new Point(0, 0), new[] { (1, 0), (2, 0), (4, 0), (8, 0), (0, 1), (0, 2), (0, 4), (0, 8), (16, 0), (0, 16) });
        }

        public int Width { get; }

        public int Height { get; }

        public Point Transform(Point point) => _swizzle.Get(point.Y * Width + point.X);

    }
    class DatSupport
    {
        public static readonly IDictionary<int, IIndexEncoding> IndexFormats = new Dictionary<int, IIndexEncoding>
        {
            [0] = ImageFormats.I8()
        };
        public static EncodingDefinition GetEncodingDefinition()
        {
            EncodingDefinition definition = new EncodingDefinition();
            definition.AddPaletteEncoding(0, new Rgba(5, 5, 5, "BGR"));
            definition.AddIndexEncodings(IndexFormats.Select(x => (x.Key, new IndexEncodingDefinition(x.Value, new[] { 0 }))).ToArray());

            return definition;
        }
    }


}
