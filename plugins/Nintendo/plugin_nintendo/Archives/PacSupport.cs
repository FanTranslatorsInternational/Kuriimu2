using System.IO;
using Komponent.IO.Attributes;
using Kontract.Kompression.Configuration;
using Kontract.Models.Archive;
using Kryptography.Hash.Crc;
#pragma warning disable 649

namespace plugin_nintendo.Archives
{
    class PacHeader
    {
        [FixedLength(4)]
        public string magic;
        public int unk1;
        public int unk2;
        public int dataOffset;
    }

    class PacTableInfo
    {
        public int unpaddedFileSize;
        public int assetCount;
        public int entryCount;
        public int stringCount;
        public int fileCount;
        public long zero0;
        public long zero1;
        public int assetOffset;
        public int entryOffset;
        public int stringOffset;
        public int fileOffset;
    }

    class PacAsset
    {
        public int stringOffset;
        public uint fnvHash;
        public int count;
        public int entryOffset;
    }

    class PacEntry
    {
        public int stringOffset;
        public uint fnvHash;
        public int extensionOffset;
        public uint extensionFnvHash;

        public int offset;
        public int decompSize;
        public int compSize;
        public int compSize2;

        public long zero0;
        public int unk1;
        public int zero1;
    }

    class PacArchiveFileInfo : ArchiveFileInfo
    {
        private static Crc32 Crc = Crc32.Default;

        public PacEntry Entry { get; }

        public PacArchiveFileInfo(Stream fileData, string filePath, PacEntry entry, IKompressionConfiguration configuration, long decompressedSize) :
            base(fileData, filePath, configuration, decompressedSize)
        {
            Entry = entry;
        }

        public uint GetHash()
        {
            var finalStream = GetFinalStream();
            finalStream.Position = 0;

            return Crc.ComputeValue(finalStream);
        }
    }
}
