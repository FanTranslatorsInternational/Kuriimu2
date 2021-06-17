using Komponent.IO.Attributes;
using Kontract.Models.IO;

namespace plugin_criware.Archives
{
    [Endianness(ByteOrder = ByteOrder.BigEndian)]
    [Alignment(0x800)]
    class CvmHeader
    {
        [FixedLength(4)]
        public string magic = "CVMH";

        public long headerSize;

        [FixedLength(0x10)]
        public byte[] reserved1;

        public long fileSize;

        [FixedLength(7)]
        public byte[] date;
        public byte padding1;

        public int version1;
        public int flags;   // 0x10 if encrypted

        [FixedLength(4)] 
        public string rofsMagic="ROFS";

        [FixedLength(0x40)] 
        public string makeToolId;

        public int version2;
        public byte unk1;
        public byte unk2;
        public short unk3;

        public int sectorCount;
        public int zoneSector;
        public int isoSectorStart;

        [FixedLength(0x74)] 
        public byte[] padding;

        [VariableLength(nameof(sectorCount))] 
        public int[] sectorCounts;

        public bool IsEncrypted => (flags & 0x10) > 0;
    }

    [Endianness(ByteOrder = ByteOrder.BigEndian)]
    [Alignment(0x800)]
    class CvmZoneInfo
    {
        [FixedLength(4)] 
        public string magic;

        public int unk1;
        public int unk2;
        public int unk3;
        [FixedLength(8)]
        public byte[] unk4;

        public int sectorLength1;
        public int sectorLength2;
        public CvmZoneDataLoc dataLoc1;
        public CvmZoneDataLoc isoDataLoc;
    }

    class CvmZoneDataLoc
    {
        public int sectorindex;
        public long length;
    }

    class IsoPrimaryDescriptor
    {
        public byte type;
        [FixedLength(5)]
        public string id;
        public byte version;
        public byte unused1;

        [FixedLength(0x20)]
        public string system_id;
        [FixedLength(0x20)]
        public string volume_id;
        [FixedLength(8)]
        public byte[] unused2;

        [Endianness(ByteOrder = ByteOrder.LittleEndian)]
        public int volSizeLe;
        [Endianness(ByteOrder = ByteOrder.BigEndian)]
        public int volSizeBe;

        [FixedLength(0x20)]
        public byte[] escapeSequences;

        public int volSetSize;
        public int volSequenceNumber;
        public short logicalBlockSizeLe;
        public short logicalBlockSizeBe;

        [Endianness(ByteOrder = ByteOrder.LittleEndian)]
        public int pathTableSizeLe;
        [Endianness(ByteOrder = ByteOrder.BigEndian)]
        public int pathTableSizeBe;

        public int typelPathTable;
        public int optTypelPathTable;
        public int typemPathTable;
        public int optTypemPathTable;

        public IsoDirectoryRecord rootDirRecord;

        [FixedLength(0x80)] public string volumeSetId;
        [FixedLength(0x80)] public string publisherId;
        [FixedLength(0x80)] public string preparerId;
        [FixedLength(0x80)] public string applicationId;

        [FixedLength(0x25)] public string copyrightFileId;
        [FixedLength(0x25)] public string abstractFileId;
        [FixedLength(0x25)] public string bibliographicFileId;

        [FixedLength(0x11)] public string creationDate;
        [FixedLength(0x11)] public string modificationDate;
        [FixedLength(0x11)] public string expirationDate;
        [FixedLength(0x11)] public string effectiveDate;

        public byte fileStructureVersion;
        public byte unused4;

        [FixedLength(0x200)]
        public byte[] applicationData;
    }

    [Alignment(2)]
    class IsoDirectoryRecord
    {
        public byte length;
        public byte extAttributeLength;

        [Endianness(ByteOrder = ByteOrder.LittleEndian)]
        public uint extentLe;
        [Endianness(ByteOrder = ByteOrder.BigEndian)]
        public uint extentBe;

        [Endianness(ByteOrder = ByteOrder.LittleEndian)]
        public uint sizeLe;
        [Endianness(ByteOrder = ByteOrder.BigEndian)]
        public uint sizeBe;

        [FixedLength(7)]
        public byte[] date;

        public byte flags;
        public byte fileUnitSize;
        public byte interleave;
        public int volumeSequenceNumber;

        public byte nameLength;
        [VariableLength(nameof(nameLength))]
        public string name;
    }

    class CvmSupport
    {
    }
}
