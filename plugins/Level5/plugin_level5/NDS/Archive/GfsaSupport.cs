using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_level5.NDS.Archive
{
    struct GfsaHeader
    {
        public string magic;

        public int directoryOffset;
        public int fileOffset;
        public int unkOffset;
        public int stringOffset;
        public int fileDataOffset;

        public int directoryCount;
        public int fileCount;
        public int decompressedTableSize;   // Summed sizes of decompressed tables
        public int unk4;
    }

    struct GfsaDirectoryEntry
    {
        public ushort hash;
        public short fileCount;
        public int fileIndex;
    }

    public class GfsaFileEntry
    {
        public ushort hash;
        public ushort offLow;
        public ushort sizeLow;
        public byte offHigh;
        public byte sizeHigh;

        public int Offset
        {
            get => (offLow | (offHigh << 16)) << 2;
            set
            {
                offLow = (ushort)((value >> 2) & 0xFFFF);
                offHigh = (byte)((value >> 18) & 0xFF);
            }
        }

        public int Size
        {
            get => sizeLow | ((sizeHigh & 0xF0) << 12);
            set
            {
                sizeLow = (ushort)(value & 0xFFFF);
                sizeHigh = (byte)((value & 0xF0000) >> 12);
            }
        }
    }

    class GfsaString
    {
        public string Value { get; }

        public ushort Hash { get; }

        public GfsaString(string value, ushort hash)
        {
            Value = value;
            Hash = hash;
        }
    }

    public class GfsaArchiveFile : ArchiveFile
    {
        public long CompressedSize => UsesCompression ? GetCompressedStream().Length : FileSize + 4;

        public GfsaFileEntry Entry { get; }

        public GfsaArchiveFile(ArchiveFileInfo fileInfo, GfsaFileEntry entry) : base(fileInfo)
        {
            Entry = entry;
        }
    }
}
