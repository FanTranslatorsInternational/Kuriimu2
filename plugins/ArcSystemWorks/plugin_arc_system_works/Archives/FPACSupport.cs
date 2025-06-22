namespace plugin_arc_system_works.Archives
{
    class FPACTableStructure
    {
        public FPACHeader header;
        public FPACEntry[] entries;
    }

    class FPACHeader
    {
        public string magic;
        public int dataOffset;
        public int fileSize;
        public int fileCount;
        public int unk1;
        public int nameBufferSize;
    }

    class FPACEntry
    {
        public string fileName;
        public int fileId;
        public int offset;
        public int size;
    }
}
