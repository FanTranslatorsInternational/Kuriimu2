using Kanvas;
using Kanvas.Contract.Encoding;
using Kanvas.Encoding;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Contract.Plugin.File.Image;
using Konnect.Plugin.File.Image;

namespace plugin_bandai_namco.Images
{
    class VtxpHeader
    {
        public string magic = "VTXP";
        public int version = 0x00010000;
        public int imgCount;
        public int hashOffset;  // Hashes are CRC32B
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

    class VtxpImageFile : ImageFile
    {
        public VtxpImageEntry Entry { get; }

        public VtxpImageFile(ImageFileInfo imageInfo, IEncodingDefinition encodingDefinition, VtxpImageEntry entry) : base(imageInfo, encodingDefinition)
        {
            Entry = entry;
        }

        public VtxpImageFile(ImageFileInfo imageInfo, bool lockImage, IEncodingDefinition encodingDefinition, VtxpImageEntry entry) : base(imageInfo, lockImage, encodingDefinition)
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
            definition.AddIndexEncodings(IndexFormats.Select(x => ((int)x.Key, new IndexEncodingDefinition
            {
                IndexEncoding = x.Value,
                PaletteEncodingIndices = PaletteFormats.Keys.Select(x => (int)x).ToArray()
            })).ToArray());

            return definition;
        }
    }
}
