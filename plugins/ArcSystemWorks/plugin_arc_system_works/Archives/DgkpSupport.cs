using System.IO;
using Komponent.IO.Attributes;
using Kontract.Kompression.Configuration;
using Kontract.Models.Archive;

namespace plugin_arc_system_works.Archives
{
    class DgkpHeader
    {
        [FixedLength(4)] 
        public string magic = "DGKP";

        public int unk1;
        public int unk2;
        public int fileCount;
        public int entryOffset;
    }

    class DgkpFileEntry
    {
        [FixedLength(4)]
        public string magic;

        public int entrySize = 0x90;
        public int size;
        public int offset;
        [FixedLength(0x80)]
        public string name;
    }

    class DgkpArchiveFileInfo:ArchiveFileInfo
    {
        public DgkpFileEntry Entry { get; }

        public DgkpArchiveFileInfo(Stream fileData, string filePath,DgkpFileEntry entry) : base(fileData, filePath)
        {
            Entry = entry;
        }

        public DgkpArchiveFileInfo(Stream fileData, string filePath, IKompressionConfiguration configuration, long decompressedSize) : base(fileData, filePath, configuration, decompressedSize)
        {
        }
    }
}
