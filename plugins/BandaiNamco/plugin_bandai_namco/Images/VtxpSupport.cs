using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas;
using Kanvas.Encoding;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Models.Image;

namespace plugin_bandai_namco.Images
{
    class VtxpHeader
    {
        [FixedLength(4)]
        public string magic = "VTXP";
        public int version = 0x00010000;
        public int imgCount;
        public int hashOffset;  // Hashes are CRC32B

        [FixedLength(0x10)]
        public byte[] padding = new byte[0x10];
    }

    class VtxpImageEntry
    {
        public int nameOffset;
        public int dataSize;
        public int paletteOffset;
        public int dataOffset;

        public uint format;
        public short width;
        public short height;
        public byte mipLevel;
        public byte type;
        public short unk1;

        public int unk2;
    }

    class VtxpImageInfo : ImageInfo
    {
        public VtxpImageEntry Entry { get; }

        public VtxpImageInfo(byte[] imageData, int imageFormat, Size imageSize, VtxpImageEntry entry) : base(imageData, imageFormat, imageSize)
        {
            Entry = entry;
        }
    }

    class VtxpSupport
    {
        private static readonly IDictionary<uint, IColorEncoding> ColorFormats = new Dictionary<uint, IColorEncoding>
        {
            [0x0C001000] = new Rgba(8, 8, 8, 8, "ARGB"),
        };

        private static readonly IDictionary<uint, IIndexEncoding> IndexFormats = new Dictionary<uint, IIndexEncoding>
        {
            [0x94000000] = ImageFormats.I4(),
            [0x95000000] = ImageFormats.I8()
        };

        private static readonly IDictionary<uint, IColorEncoding> PaletteFormats = new Dictionary<uint, IColorEncoding>
        {
            [0x0000] = new Rgba(8, 8, 8, 8, "ABGR"),
            [0x1000] = new Rgba(8, 8, 8, 8, "ARGB"),
            [0x2000] = new Rgba(8, 8, 8, 8, "RGBA"),
            [0x3000] = new Rgba(8, 8, 8, 8, "BGRA"),
            [0x4000] = new Rgba(8, 8, 8, 8, "XBGR"),
            [0x5000] = new Rgba(8, 8, 8, 8, "XRGB"),
            [0x6000] = new Rgba(8, 8, 8, 8, "RGBX"),
            [0x7000] = new Rgba(8, 8, 8, 8, "BGRX")
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(ColorFormats.Select(x => ((int)x.Key, x.Value)).ToArray());

            definition.AddPaletteEncodings(PaletteFormats.Select(x => ((int)x.Key, x.Value)).ToArray());
            definition.AddIndexEncodings(IndexFormats.Select(x => ((int)x.Key, new IndexEncodingDefinition(x.Value, PaletteFormats.Keys.Select(x => (int)x).ToArray()))).ToArray());

            return definition;
        }
    }
}
