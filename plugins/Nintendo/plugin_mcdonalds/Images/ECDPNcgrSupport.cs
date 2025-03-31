using Komponent.Contract.Aspects;

namespace plugin_mcdonalds.Images
{
    class NitroCharHeader
    {
        [FixedLength(4)]
        public string magic;
        public int sectionSize;
        public short tileCountX;
        public short tileCountY;
        public int imageFormat;
        public short unk1;
        public short unk2;
        public int tiledFlag;
        public int tileDataSize;
        public int unk3;
    }

    class NitroTtlpHeader
    {
        [FixedLength(4)]
        public string magic;
        public int sectionSize;
        public int colorDepth;
        public int unk1;
        public int paletteSize;
        public int colorsPerPalette;
    }

    class NitroHeader
    {
        [FixedLength(4)]
        public string magic;
        public ushort byteOrder;
        public short unk1;
        public int sectionSize;
        public short headerSize;
        public short sectionCount;
    }
}
