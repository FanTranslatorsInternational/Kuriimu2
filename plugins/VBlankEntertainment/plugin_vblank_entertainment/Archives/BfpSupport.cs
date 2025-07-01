namespace plugin_vblank_entertainment.Archives
{
    class BfpHeader
    {
        public string magic;
        public int entryCount;
        public int unk1;
        public int unk2;
    }

    class BfpFileEntry
    {
        public uint hash;
        public int offset;
        public int decompSize;
    }

    class BfpBucketFileEntry
    {
        public int offset;
        public int decompSize;
    }
}
