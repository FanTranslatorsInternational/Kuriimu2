using System.IO;
using Komponent.IO.Attributes;
using Kontract.Models.Archive;

namespace plugin_nippon_ichi.Archives
{
    class DatHeader
    {
        [FixedLength(8)] 
        public string magic = "NISPACK\0";
        public int zero0;
        public int fileCount;
    }

    class DatEntry
    {
        [FixedLength(0x20)]
        public string name;
        public int offset;
        public int size;
        public uint unk1;
    }

    class DatArchiveFileInfo : ArchiveFileInfo
    {
        public DatEntry Entry { get; }

        public DatArchiveFileInfo(Stream fileData, string filePath, DatEntry entry) : base(fileData, filePath)
        {
            Entry = entry;
        }
    }
}
