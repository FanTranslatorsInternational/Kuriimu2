using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_sony.Archives
{
    // Specification overview: https://formats.kaitai.io/iso9660/iso9660.svg

    class IsoVolumeDescriptor
    {
        public byte type;
        public string magic = "CD001";
        public byte version;
        public IsoPrimaryVolumeDescriptor descriptorPrimary;
    }

    class IsoPrimaryVolumeDescriptor
    {
        public byte zero0;
        public string systemId;
        public string volumeId;
        public long zero1;
        public IsoUInt32 spaceSize;
        public byte[] zero2;

        public IsoUInt16 setSize;
        public IsoUInt16 seqCount;
        public IsoUInt16 logicalBlockSize;
        public IsoUInt32 pathTableSize;
        public IsoLbaPathTable pathTable;

        public IsoDirEntry rootDir;

        public string volumeSetId;
        public string publisherId;
        public string dataPreparerId;
        public string applicationId;
        public string copyrightFileId;
        public string abstractFileId;
        public string bibliographicFileId;

        public IsoDecDateTime createDateTime;
        public IsoDecDateTime modDateTime;
        public IsoDecDateTime expireDateTime;
        public IsoDecDateTime effectiveDateTime;

        public byte fileStructureVersion;
        public byte zero3;
    }

    class IsoUInt32
    {
        public uint valueLe;
        public uint valueBe;
    }

    class IsoUInt16
    {
        public ushort valueLe;
        public ushort valueBe;
    }

    class IsoLbaPathTable
    {
        public uint lbaPathTableLe;
        public uint optLbaPathTableLe;
        public uint lbaPathTableBe;
        public uint optLbaPathTableBe;
    }

    class IsoDirEntry
    {
        public byte length;
        public IsoDirEntryBody body;

        public bool IsDirectory => (body.flags & 0x02) > 0;
    }

    class IsoDirEntryBody
    {
        public byte attributeLength;
        public IsoUInt32 lbaExtent;
        public IsoUInt32 sizeExtent;
        public IsoDatetime dateTime;
        public byte flags;
        public byte unitSize;
        public byte gapSize;
        public IsoUInt16 seqCount;
        public byte fileNameLength;
        public string fileName = "";
    }

    class IsoDatetime
    {
        public byte year;
        public byte month;
        public byte day;
        public byte hour;
        public byte minute;
        public byte second;
        public byte timezone;
    }

    class IsoDecDateTime
    {
        public string year;
        public string month;
        public string day;
        public string hour;
        public string minute;
        public string second;
        public string microSecond;
        public byte timeZone;
    }

    class Ps2DiscArchiveFile : ArchiveFile
    {
        public IsoDirEntry Entry { get; }

        public Ps2DiscArchiveFile(ArchiveFileInfo fileInfo, IsoDirEntry entry) : base(fileInfo)
        {
            Entry = entry;
        }
    }
}
