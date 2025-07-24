using Kanvas;
using Kanvas.Contract.Encoding;
using Komponent.Contract.Enums;
using Konnect.Plugin.File.Image;

namespace plugin_grezzo.Fonts
{
    class GzfHeader
    {
        public string magic;
        public int version;
        public ushort imgInfoOffset;
        public ushort imgInfoSize;
        public int entrySize;
        public int imgCount;
        public int entryCount;
        public uint unk2;
        public uint unk3;
        public short format;
        public short unk4;
        public short unk5;
        public short glyphWidth;
        public short glyphHeight;
    }

    class GzfImageInfo
    {
        public int offset;
        public short width;
        public short height;
    }

    class GzfEntry
    {
        public int codePoint;
        public short charWidth;
        public short imageIndex;
        public short posX;
        public byte column;
        public byte row;
    }

    class GzfSupport
    {
        public static readonly Dictionary<int, IColorEncoding> Formats = new()
        {
            [0xB] = ImageFormats.A4(BitOrder.LeastSignificantBitFirst)
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(Formats);

            return definition;
        }
    }
}
