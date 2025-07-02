using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_cavia.Archives
{
    class DpkHeader
    {
        public string magic = "dpk\0";
        public int entryOffset;
        public int unk1;
        public int fileOffset;
        public int fileCount;
    }

    class DpkEntry
    {
        public byte[] unk1;
        public int fileSize;
        public int padFileSize;
        public int fileOffset;
        public int zero0;
    }

    class DpkArchiveFile : ArchiveFile
    {
        public DpkEntry Entry { get; }

        public DpkArchiveFile(ArchiveFileInfo fileInfo, DpkEntry entry) : base(fileInfo)
        {
            Entry = entry;
        }
    }
}
