namespace plugin_mcdonalds.Images
{
    struct NitroCharHeader
    {
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

    struct NitroTtlpHeader
    {
        public string magic;
        public int sectionSize;
        public int colorDepth;
        public int unk1;
        public int paletteSize;
        public int colorsPerPalette;
    }

    struct NitroHeader
    {
        public string magic;
        public ushort byteOrder;
        public short unk1;
        public int sectionSize;
        public short headerSize;
        public short sectionCount;
    }
}
