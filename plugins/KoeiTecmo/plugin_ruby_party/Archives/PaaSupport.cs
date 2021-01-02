using System.IO;
using Komponent.IO.Attributes;
using Kontract.Kompression.Configuration;
using Kontract.Models.Archive;

namespace plugin_ruby_party.Archives
{
    class PaaHeader
    {
        [FixedLength(4)] 
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

    class PaaArchiveFileInfo : ArchiveFileInfo
    {
        public PaaEntry Entry { get; }

        public PaaArchiveFileInfo(Stream fileData, string filePath, PaaEntry entry) : base(fileData, filePath)
        {
            Entry = entry;
        }
    }
}
