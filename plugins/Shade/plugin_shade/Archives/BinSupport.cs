using Konnect.Contract.DataClasses.Plugin.File.Archive;

namespace plugin_shade.Archives
{
    class BinHeader
    {
        public int fileCount;
        public int padFactor;
        public int mulFactor;
        public int shiftFactor;
        public int mask;
    }
    
    class BinFileInfo
    {
        public uint offSize;
    }

    class BinArchiveFile : ShadeArchiveFile
    {
        public BinFileInfo Entry { get; }

        public BinArchiveFile(ArchiveFileInfo fileInfo, BinFileInfo entry) : base(fileInfo)
        {
            Entry = entry;
        }
    }
}
