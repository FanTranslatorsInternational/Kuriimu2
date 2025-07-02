namespace plugin_square_enix.Archives
{
    class DpkHeader
    {
        public int fileCount;
        public int fileSize;
    }

    class DpkEntry
    {
        public string name;
        public short nameSum;
        public int offset;
        public int compSize;
        public int decompSize;
    }
}
