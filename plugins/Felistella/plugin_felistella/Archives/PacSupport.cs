using Komponent.Contract.Aspects;
using Komponent.Contract.Enums;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_felistella.Archives
{
    class PacHeader
    {
        public byte unk1;
        public byte unk2;
        public byte pacFormat;
        public byte unk3;

        public int unk4;
        public int fileSize;
        public int dataSize;

        public int unk5;
        public int blockCount; // One block is 0x20 bytes

        public int nameCount;
        public int nameOffset;

        public int entryCount;
        public int entryOffset;

        public int unkCount1;
        public int unkOffset1;

        public int unkCount2;
        public int unkOffset2;
    }

    class PacDirectoryEntry
    {
        public string name;
        public short entryCount;
        public short entryIndex;
    }

    [Endianness(ByteOrder = ByteOrder.BigEndian)]
    [BitFieldInfo(BitOrder = BitOrder.MostSignificantBitFirst)]
    class PacEntry
    {
        [BitField(24)]
        public int offset;
        [BitField(24)]
        public int size;
        [BitField(16)]
        public short flags;
    }

    class PacArchiveFile : ArchiveFile
    {
        public PacEntry Entry { get; }

        public PacArchiveFile(ArchiveFileInfo fileInfo, PacEntry entry) : base(fileInfo)
        {
            Entry = entry;
        }
    }
}
