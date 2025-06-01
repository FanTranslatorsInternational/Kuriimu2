using Kanvas;
using Komponent.Contract.Aspects;
using Konnect.Plugin.File.Image;

namespace plugin_level5.N3DS.Image
{
    struct ZtexHeader
    {
        public string magic;
        public short imageCount;
        public short flags;

        public bool HasExtendedEntries => (flags & 1) != 0;

        public bool HasUnknownEntries => (flags & 2) != 0;
    }

    struct ZtexEntry
    {
        public string name;
        public uint crc32;
        public int offset;
        public int zero1;
        public int dataSize;
        public short width;
        public short height;
        public byte mipCount;
        public byte format;
        public short unk3; // 0xFF
    }

    struct ZtexUnkEntry
    {
        public int unk0;
        public int zero0;
    }

    class ZtexSupport
    {
        public static EncodingDefinition GetEncodingDefinition()
        {
            var encodingDefinition = new EncodingDefinition();
            encodingDefinition.AddColorEncoding(0x01, ImageFormats.Rgb565());
            encodingDefinition.AddColorEncoding(0x03, ImageFormats.Rgb888());
            encodingDefinition.AddColorEncoding(0x05, ImageFormats.Rgba4444());
            encodingDefinition.AddColorEncoding(0x07, ImageFormats.Rgba5551());
            encodingDefinition.AddColorEncoding(0x09, ImageFormats.Rgba8888());
            encodingDefinition.AddColorEncoding(0x0B, ImageFormats.L4());
            encodingDefinition.AddColorEncoding(0x0D, ImageFormats.Al44());
            encodingDefinition.AddColorEncoding(0x11, ImageFormats.A8());
            encodingDefinition.AddColorEncoding(0x13, ImageFormats.L8());
            encodingDefinition.AddColorEncoding(0x15, ImageFormats.La88());
            encodingDefinition.AddColorEncoding(0x18, ImageFormats.Etc1(true));
            encodingDefinition.AddColorEncoding(0x19, ImageFormats.Etc1A4(true));

            return encodingDefinition;
        }
    }
}
