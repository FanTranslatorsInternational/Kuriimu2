namespace plugin_atlus.N3DS.Image
{
    struct Spr3Header
    {
        public int const0;
        public int const1;
        public string magic;
        public int headerSize;
        public int unk1;
        public short imgCount;
        public short entryCount;
        public int imgOffset;
        public int entryOffset;
    }

    struct Spr3Offset
    {
        public int zero1;
        public int offset;
    }
}
