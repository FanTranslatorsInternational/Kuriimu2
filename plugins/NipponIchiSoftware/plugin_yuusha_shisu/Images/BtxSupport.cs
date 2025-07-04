using Kanvas;
using Kanvas.Contract.Encoding;
using Komponent.Contract.Enums;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Plugin.File.Image;

namespace plugin_yuusha_shisu.Images
{
    public class BtxHeader
    {
        public string magic;
        public int clrCount; // Palette size?
        public short width;
        public short height;
        public int unk1;
        public byte format;
        public byte swizzleMode;
        public byte mipLevels;
        public byte unk2;
        public int unk4;
        public int dataOffset;
        public int unk5;
        public int paletteOffset;
        public int nameOffset;
    }

    public class BtxSupport
    {
        public static readonly IDictionary<int, IColorEncoding> ColorFormats = new Dictionary<int, IColorEncoding>
        {
            [0] = ImageFormats.Rgba8888(ByteOrder.BigEndian),
            [1] = ImageFormats.Rgb888()
        };

        public static readonly IDictionary<int, IndexEncodingDefinition> IndexFormats = new Dictionary<int, IndexEncodingDefinition>
        {
            [5] = new() { IndexEncoding = ImageFormats.I8(), PaletteEncodingIndices = [0] }
        };

        private static readonly Dictionary<int, IColorEncoding> PaletteFormats = new Dictionary<int, IColorEncoding>
        {
            [0] = ImageFormats.Rgba8888(ByteOrder.BigEndian)
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();

            definition.AddColorEncodings(ColorFormats);
            definition.AddPaletteEncodings(PaletteFormats);
            definition.AddIndexEncodings(IndexFormats);

            return definition;
        }

        public static int GetBitDepth(int format)
        {
            return ColorFormats.TryGetValue(format, out IColorEncoding? colorFormat) 
                ? colorFormat.BitDepth 
                : IndexFormats[format].IndexEncoding.BitDepth;
        }
    }
}
