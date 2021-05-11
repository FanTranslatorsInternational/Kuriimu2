namespace plugin_inti_creates.Archives
{
    class DatHeader
    {
        public int dataOffset;
        public int zero1;
        public int fileSize;
        public int decompSize;
        public int zero2;
        public int zero3;
    }

    class DatSubHeader
    {
        public int fileCount;
        public int unk1;
        public int zero1;
        public int dataOffset;
        public int dataSize;
    }
}
