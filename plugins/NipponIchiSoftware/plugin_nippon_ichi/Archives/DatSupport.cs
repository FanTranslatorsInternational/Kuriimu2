using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_nippon_ichi.Archives
{
    class DatHeader
    {
        public string magic = "NISPACK\0";
        public int zero0;
        public int fileCount;
    }

    class DatEntry
    {
        public string name;
        public int offset;
        public int size;
        public uint unk1;
    }

    class DatArchiveFile : ArchiveFile
    {
        public DatEntry Entry { get; }

        public DatArchiveFile(ArchiveFileInfo fileInfo, DatEntry entry) : base(fileInfo)
        {
            Entry = entry;
        }
    }
}
