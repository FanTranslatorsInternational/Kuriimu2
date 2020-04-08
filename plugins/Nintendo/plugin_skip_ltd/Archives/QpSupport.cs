namespace plugin_skip_ltd.Archives
{
    class QpHeader
    {
        public uint hash;
        public int entryDataOffset;
        public int entryDataSize;
        public int dataOffset;
    }

    class QpEntry
    {
        public int tmp1;
        public int fileOffset;
        public int fileSize;

        public bool IsDirectory
        {
            get => tmp1 >> 24 == 1;
            set => tmp1 = (tmp1 & 0xFFFFFF) | ((value ? 1 : 0) << 24);
        }

        public int NameOffset
        {
            get => tmp1 & 0xFFFFFF;
            set => tmp1 = (tmp1 & ~0xFFFFFF) | (value & 0xFFFFFF);
        }
    }
}
