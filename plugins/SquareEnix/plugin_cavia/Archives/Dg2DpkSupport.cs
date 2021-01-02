using System.IO;
using Komponent.IO.Attributes;
using Kontract.Kompression.Configuration;
using Kontract.Models.Archive;

namespace plugin_cavia.Archives
{
    class DpkHeader
    {
        [FixedLength(4)]
        public string magic = "dpk\0";

        public int entryOffset;
        public int unk1;
        public int fileOffset;
        public int fileCount;
    }

    class DpkEntry
    {
        [FixedLength(0x10)] 
        public byte[] unk1;

        public int fileSize;
        public int padFileSize;
        public int fileOffset;
        public int zero0;
    }

    class DpkArchiveFileInfo : ArchiveFileInfo
    {
        public DpkEntry Entry { get; }

        public DpkArchiveFileInfo(Stream fileData, string filePath, DpkEntry entry) : 
            base(fileData, filePath)
        {
            Entry = entry;
        }
    }
}
