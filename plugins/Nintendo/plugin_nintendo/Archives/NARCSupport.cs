namespace plugin_nintendo.Archives
{
    struct NarcHeader
    {
        public string magic; // NARC
        public ushort bom;
        public short version; // 0x100
        public int fileSize; 
        public short chunkSize; // 0x10
        public short chunkCount; // 0x3
    }

    struct NarcFatHeader
    {
        public string magic; // BTAF
        public int chunkSize;
        public short fileCount;
        public short reserved1;
    }

    struct NarcFntHeader
    {
        public string magic; // BTNF
        public int chunkSize;
    }
}
