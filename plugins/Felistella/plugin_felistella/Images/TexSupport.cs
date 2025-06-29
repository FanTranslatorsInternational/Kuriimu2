using Kanvas;
using Kanvas.Contract.Encoding;
using Komponent.Contract.Enums;
using Konnect.Plugin.File.Image;

namespace plugin_felistella.Images
{
    class TexHeader
    {
        public int unk1;
        public int unk2;
        public int fileSize;
        public uint unk3;

        public int headerSize;
        public int dataStart;
        public int dataSize;
        public int zero0;

        public int entryOffset;
        public int unk4;
    }

    class TexEntry
    {
        public int unk1;
        public short width;
        public short height;
        public int dataOffset;
        public int dataSize;
        public int zero0;
        public int zero1;
        public int unk2;
        public int unk3;
    }

    class TexSupport
    {
        public static IDictionary<int, IColorEncoding> Formats = new Dictionary<int, IColorEncoding>
        {
            [0] = ImageFormats.Rgba8888(ByteOrder.BigEndian)
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(Formats);

            return definition;
        }
    }
}
