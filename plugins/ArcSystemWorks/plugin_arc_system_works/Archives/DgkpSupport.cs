using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_arc_system_works.Archives
{
    class DgkpHeader
    {
        public string magic = "DGKP";
        public int unk1;
        public int unk2;
        public int fileCount;
        public int entryOffset;
    }

    class DgkpFileEntry
    {
        public string magic;
        public int entrySize = 0x90;
        public int size;
        public int offset;
        public string name;
    }

    class DgkpArchiveFile : ArchiveFile
    {
        public DgkpFileEntry Entry { get; }

        public DgkpArchiveFile(ArchiveFileInfo fileInfo, DgkpFileEntry entry) : base(fileInfo)
        {
            Entry = entry;
        }
    }
}
