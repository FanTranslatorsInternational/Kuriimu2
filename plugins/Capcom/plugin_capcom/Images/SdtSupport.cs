using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas;
using Kanvas.Encoding;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Models.Image;

namespace plugin_dotemu.Images
{
    class SdtHeader
    {
        [FixedLength(8)]
        public string magic;

        public byte unk1;
        public byte unk2;
        public byte unk3;
        public byte unk4;

        public int imageOffset;
        public int imageSize;

        public int paletteOffset;
        public int paletteSize;
        
        public int width;
        public int height;
    }
    class SdtSupport
    {
        public static readonly IDictionary<int, IIndexEncoding> IndexFormats = new Dictionary<int, IIndexEncoding>
        {
            [0x01] = new Index(5,3, "AI"),
            [0x03] = ImageFormats.I4(Kontract.Models.IO.BitOrder.LeastSignificantBitFirst),
            [0x04] = ImageFormats.I8(),
        };
        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddPaletteEncoding(0, new Rgba(5, 5, 5, "BGR"));

            definition.AddIndexEncodings(IndexFormats.Select(x => (x.Key, new IndexEncodingDefinition(x.Value, new[] { 0 }))).ToArray());
            return definition;
        }
    }


}
