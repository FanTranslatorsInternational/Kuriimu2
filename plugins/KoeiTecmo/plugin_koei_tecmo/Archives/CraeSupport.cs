namespace plugin_koei_tecmo.Archives
{
    class CraeHeader
    {
        public string magic;
        public int unk1;
        public int dataSize;
        public int entryOffset;
        public int entrySize;
        public int fileCount;
        public int unk2;
    }

    class CraeEntry
    {
        public int offset;
        public int size;
        public string name;
    }
}
