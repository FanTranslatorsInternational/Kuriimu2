using System.IO;
using Komponent.IO.Attributes;
using Kontract.Kompression.Configuration;
using Kontract.Models.Archive;
#pragma warning disable 649

namespace plugin_konami.Archives
{
    class TarcHeader
    {
        [FixedLength(4)]
        public string magic;
        public int fileSize;
        public int fileCount;
        public int unk1;
        public int unk2;
        public int entryOffset;
        public int entrySecSize;
        public int nameOffset;
        public int nameSecSize;
    }

    [Alignment(0x10)]
    class TarcEntry
    {
        public int unk1;
        public int nameOffset;
        public int fileOffset;
        public int decompSize;
        public int compSize;
        public int unk2;
    }

    class TarcArchiveFileInfo : ArchiveFileInfo
    {
        public TarcEntry Entry { get; }

        public TarcArchiveFileInfo(Stream fileData, string filePath, TarcEntry entry) :
            base(fileData, filePath)
        {
            Entry = entry;
        }

        public TarcArchiveFileInfo(Stream fileData, string filePath, TarcEntry entry, IKompressionConfiguration configuration, long decompressedSize) :
            base(fileData, filePath, configuration, decompressedSize)
        {
            Entry = entry;
        }
    }
}
