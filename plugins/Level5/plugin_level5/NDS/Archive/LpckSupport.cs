namespace plugin_level5.NDS.Archive
{
    class LpckHeader
    {
        public int headerSize;
        public int totalSize;
        public int fileCount;
        public string magic;
    }

    class LpckEntry
    {
        public int headerSize;
        public int totalSize;
        public int zero;
        public int fileSize;
        public string name;
    }
}
