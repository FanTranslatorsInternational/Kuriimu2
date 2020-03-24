using System.IO;
using Komponent.IO.Attributes;
using Kontract.Kompression.Configuration;
using Kontract.Models.Archive;

namespace plugin_koei_tecmo.Archives
{
    class X3Header
    {
        [FixedLength(4)]
        public string magic;
        public int fileCount;
        public int fileAlignment;
    }

    class X3FileEntry
    {
        public long offset;
        public int compressedSize;
        public int decompressedSize;

        public bool IsCompressed => compressedSize != decompressedSize && decompressedSize > 0;
    }

    class X3ArchiveFileInfo : ArchiveFileInfo
    {
        public X3FileEntry Entry { get; }

        public X3ArchiveFileInfo(Stream fileData, string filePath, X3FileEntry entry) : base(fileData, filePath)
        {
            Entry = entry;
        }

        public X3ArchiveFileInfo(Stream fileData, string filePath,
            IKompressionConfiguration configuration, long decompressedSize,
            X3FileEntry entry) :
            base(fileData, filePath, configuration, decompressedSize)
        {
            Entry = entry;
        }
    }
}
