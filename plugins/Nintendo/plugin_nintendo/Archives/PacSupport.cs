using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;
using Kryptography.Checksum.Crc;

namespace plugin_nintendo.Archives
{
    struct PacHeader
    {
        public string magic;
        public int unk1;
        public int unk2;
        public int dataOffset;
    }

    struct PacTableInfo
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

    struct PacAsset
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

    class PacArchiveFile : ArchiveFile
    {
        private static Crc32 Crc = Crc32.Crc32B;

        public PacEntry Entry { get; }

        public PacArchiveFile(ArchiveFileInfo fileInfo, PacEntry entry) : base(fileInfo)
        {
            Entry = entry;
        }

        public uint GetHash()
        {
            Stream finalStream = GetFinalStream();
            finalStream.Position = 0;

            return Crc.ComputeValue(finalStream);
        }
    }
}
