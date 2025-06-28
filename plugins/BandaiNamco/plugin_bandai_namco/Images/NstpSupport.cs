using Kanvas;
using Kanvas.Contract.Encoding;
using Kanvas.Encoding;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Contract.Plugin.File.Image;
using Konnect.Plugin.File.Image;

namespace plugin_bandai_namco.Images
{
    class NstpHeader
    {
        public string magic;
        public int version = 0x00010000;
        public int imgCount;
        public int hashOffset;  // Hashes are CRC32B
    }

    class NstpImageEntry
    {
        public int nameOffset;
        public int dataSize;
        public int dataOffset;
        public int format;

        public short width;
        public short height;
        public byte mipLevels;
        public byte swizzleMode;
        public short unk2;
        public int unk3;
        public int unk4;
    }

    class NstpImageFile : ImageFile
    {
        public NstpImageEntry Entry { get; }

        public NstpImageFile(ImageFileInfo imageInfo, IEncodingDefinition encodingDefinition, NstpImageEntry entry) : base(imageInfo, encodingDefinition)
        {
            Entry = entry;
        }

        public NstpImageFile(ImageFileInfo imageInfo, bool lockImage, IEncodingDefinition encodingDefinition, NstpImageEntry entry) : base(imageInfo, lockImage, encodingDefinition)
        {
            Entry = entry;
        }
    }

    class NstpSupport
    {
        private static readonly IDictionary<int, IColorEncoding> NstpFormats = new Dictionary<int, IColorEncoding>
        {
            [0x01] = ImageFormats.L8(),
            [0x04] = new Rgba(5, 5, 5, 1, "ARGB"),
            [0x05] = new Rgba(4, 4, 4, 4, "ARGB"),
            [0x06] = new Rgba(8, 8, 8, 8, "ARGB"),
            [0x07] = new Rgba(8, 8, 8, 8, "ABGR")
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(NstpFormats);

            return definition;
        }
    }
}
