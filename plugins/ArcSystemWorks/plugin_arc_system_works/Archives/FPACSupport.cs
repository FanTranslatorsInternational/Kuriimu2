using Komponent.IO.Attributes;
#pragma warning disable 649

namespace plugin_arc_system_works.Archives
{
    class FPACTableStructure
    {
        public FPACHeader header;
        [VariableLength("header.fileCount")]
        public FPACEntry[] entries;
    }

    [Alignment(0x10)]
    class FPACHeader
    {
        [FixedLength(4)]
        public string magic;

        public int dataOffset;
        public int fileSize;
        public int fileCount;
        public int unk1;
        public int nameBufferSize;
    }

    [Alignment(0x10)]
    class FPACEntry
    {
        [VariableLength("..header.nameBufferSize")]
        public string fileName;

        public int fileId;
        public int offset;
        public int size;
    }
}
