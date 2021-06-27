using System.IO;
using Komponent.IO.Attributes;
using Kontract.Kompression.Configuration;
using Kontract.Models.Archive;

namespace plugin_ganbarion.Archives
{
    class JcmpHeader
    {
        [FixedLength(4)]
        public string magic="jCMP";

        public int fileSize;
        public int unk1;
        public int compSize;
        public int decompSize;
    }

    class JarcHeader
    {
        [FixedLength(4)]
        public string magic = "jARC";
        public int fileSize;
        public int unk1;
        public int fileCount;
    }

    class JarcEntry
    {
        public int fileOffset;
        public int fileSize;
        public int nameOffset;
        public uint hash;
        public int unk1;
    }

    class JarcArchiveFileInfo : ArchiveFileInfo
    {
        public JarcEntry Entry { get; }

        public JarcArchiveFileInfo(Stream fileData, string filePath,JarcEntry entry) : base(fileData, filePath)
        {
            Entry = entry;
        }

        public JarcArchiveFileInfo(Stream fileData, string filePath, IKompressionConfiguration configuration, long decompressedSize, JarcEntry entry) : base(fileData, filePath, configuration, decompressedSize)
        {
            Entry = entry;
        }
    }
}
