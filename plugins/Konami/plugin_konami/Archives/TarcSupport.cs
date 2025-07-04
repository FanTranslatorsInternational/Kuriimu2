using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_konami.Archives
{
    class TarcHeader
    {
        public string magic;
        public int fileSize;
        public int fileCount;
        public int unk1;

        public int unk2;
        public int entryOffset;
        public int entrySecSize;
        public int nameOffset;

        public int nameSecSize;
        public int unk3;
    }

    class TarcEntry
    {
        public int unk1;
        public int nameOffset;
        public int fileOffset;
        public int decompSize;
        public int compSize;
        public int unk2;
    }

    class TarcArchiveFile : ArchiveFile
    {
        public TarcEntry Entry { get; }

        public TarcArchiveFile(ArchiveFileInfo fileInfo, TarcEntry entry) : base(fileInfo)
        {
            Entry = entry;
        }
    }
}
