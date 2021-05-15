using System.IO;
using Kontract.Kompression.Configuration;
using Kontract.Models.Archive;
#pragma warning disable 649

namespace plugin_koei_tecmo.Archives
{
    class X3Header
    {
        public uint magic = 0x0133781D;
        public int fileCount;
        public int offsetMultiplier = 0x20;
    }

    class X3FileEntry
    {
        public long offset;
        public int fileSize;
        public int decompressedFileSize;

        public bool IsCompressed => fileSize != decompressedFileSize && decompressedFileSize > 0;
    }

    class X3ArchiveFileInfo : ArchiveFileInfo
    {
        public X3FileEntry Entry { get; }

        public int FirstBlockSize { get; }

        public X3ArchiveFileInfo(Stream fileData, string filePath,
            X3FileEntry entry, int firstBlockSize) :
            base(fileData, filePath)
        {
            Entry = entry;
            FirstBlockSize = firstBlockSize;
        }

        public X3ArchiveFileInfo(Stream fileData, string filePath,
            IKompressionConfiguration configuration, long decompressedSize,
            X3FileEntry entry, int firstBlockSize) :
            base(fileData, filePath, configuration, decompressedSize)
        {
            Entry = entry;
            FirstBlockSize = firstBlockSize;
        }
    }
}
