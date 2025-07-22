using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

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
        public FpacFlags flags;
        public int nameBufferSize;
    }

    class FPACEntry
    {
        public string? fileName;
        public int fileId;
        public int offset;
        public int size;
        public uint hash;
    }

    [Flags]
    enum FpacFlags : uint
    {
        HasNoName = 0x000000002,
        HasHash = 0x80000000
    }

    class FpacArchiveFile : ArchiveFile
    {
        public FPACEntry Entry { get; }

        public FpacArchiveFile(ArchiveFileInfo fileInfo, FPACEntry entry) : base(fileInfo)
        {
            Entry = entry;
        }
    }
}
