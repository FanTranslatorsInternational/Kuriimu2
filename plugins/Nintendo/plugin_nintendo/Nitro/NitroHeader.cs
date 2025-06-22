namespace plugin_nintendo.Nitro
{
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
