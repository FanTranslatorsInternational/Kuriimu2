using System.Collections.Generic;
using Kanvas;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Models.Image;
using Kontract.Models.IO;
#pragma warning disable 649

namespace plugin_yuusha_shisu.Images
{
    public class BtxHeader
    {
        [FixedLength(0x4)]
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
        private static readonly IDictionary<int, IColorEncoding> ColorFormats = new Dictionary<int, IColorEncoding>
        {
            [0] = ImageFormats.Rgba8888(ByteOrder.BigEndian),
            [1] = ImageFormats.Rgb888()
        };

        private static readonly IDictionary<int, IndexEncodingDefinition> IndexFormats = new Dictionary<int, IndexEncodingDefinition>
        {
            [5] = new IndexEncodingDefinition(ImageFormats.I8(), new[] { 0 })
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
            if (ColorFormats.ContainsKey(format))
                return ColorFormats[format].BitDepth;

            return IndexFormats[format].IndexEncoding.BitDepth;
        }
    }
}
