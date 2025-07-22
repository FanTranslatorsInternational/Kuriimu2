namespace plugin_ci_games.Archives
{
    class Dpk4Header
    {
        public string magic = "DPK4";
        public uint fileSize;
        public int fileTableSize;
        public int fileCount;
    }

    class Dpk4FileEntry
    {
        public int entrySize;
        public int size;
        public int compressedSize;
        public int offset;
        public string fileName;

        public bool IsCompressed => size > compressedSize;
    }
}
