using System.IO;
using Komponent.IO.Attributes;
using Kontract.Kompression.Configuration;
using Kontract.Models.Archive;
using Kontract.Models.IO;

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
        [FixedLength(0x10)]
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

    class PacArchiveFileInfo : ArchiveFileInfo
    {
        public PacEntry Entry { get; }

        public PacArchiveFileInfo(Stream fileData, string filePath, PacEntry entry) : base(fileData, filePath)
        {
            Entry = entry;
        }
    }
}
