using Kanvas;
using Kanvas.Contract.Encoding;
using Kanvas.Encoding;
using Komponent.Contract.Enums;
using Konnect.Plugin.File.Image;

namespace plugin_level5.NDS.Image
{
    struct GtxtLtHeader
    {
        public string magic; // GTXT
        public byte indexFormat;
        public byte unk1;
        public byte unk2;
        public byte unk3;

        public short paddedWidth;
        public short paddedHeight;
        public short width;
        public short height;

        public short unkOffset1;
        public short unkCount1;

        public short unkOffset2;
        public short unkCount2;

        public short indexOffset;
        public short indexCount;

        public int dataOffset;
        public short tileCount;
        public short unk4;
    }

    struct GtxtLpHeader
    {
        public string magic; // GPLT
        public short colorCount;
        public short paletteFormat;
    }

    class GtxtSupport
    {
        public static readonly IDictionary<int, IIndexEncoding> IndexFormats = new Dictionary<int, IIndexEncoding>
        {
            [2] = ImageFormats.I4(BitOrder.LeastSignificantBitFirst),
            [3] = ImageFormats.I8()
        };

        public static readonly IDictionary<int, IColorEncoding> PaletteFormats = new Dictionary<int, IColorEncoding>
        {
            [8] = new Rgba(5, 5, 5, 1, "ABGR")
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var encodingDefinition = new EncodingDefinition(); 
            encodingDefinition.AddPaletteEncodings(PaletteFormats);

            foreach (int format in IndexFormats.Keys)
                encodingDefinition.AddIndexEncoding(format, IndexFormats[format], [8]);

            return encodingDefinition;
        }
    }
}
