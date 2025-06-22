namespace plugin_nintendo.Archives
{
    struct MMBinHeader
    {
        public int tableSize;
        public short resourceCount;
        public short unk1;
        public int unk2;
    }

    struct MMBinResourceEntry
    {
        public string resourceName;
        public int offset;
        public int metaSize;
        public int ctpkSize;
        public byte[] padding;
    }
}
