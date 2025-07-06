using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_ruby_party.Archives
{
    class PaaHeader
    {
        public string magic = "PAA\0";
        public int unk1;
        public int fileCount;
        public int entryOffset;
        public int offsetsOffset;
        public int unk2;    // Double the file Count
    }

    class PaaEntry
    {
        public int nameOffset;
        public int size;
        public int unk1;
        public int unk2;
    }

    class PaaArchiveFile : ArchiveFile
    {
        public PaaEntry Entry { get; }

        public PaaArchiveFile(ArchiveFileInfo fileInfo, PaaEntry entry) : base(fileInfo)
        {
            Entry = entry;
        }
    }
}
