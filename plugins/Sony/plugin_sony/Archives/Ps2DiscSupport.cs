using Komponent.IO.Attributes;
using Kontract.Models.Archive;
using Kontract.Models.IO;
using System.IO;
using Kontract.Kompression.Configuration;

namespace plugin_sony.Archives
{
    // Specification overview: https://formats.kaitai.io/iso9660/iso9660.svg

    class IsoVolumeDescriptor
    {
        public byte type;
        [FixedLength(5)]
        public string magic = "CD001";
        public byte version;

        // This field seems to be non existent on the PS2
        //public IsoBootRecord bootRecord;

        public IsoPrimaryVolumeDescriptor descriptorPrimary;
    }

    class IsoBootRecord
    {
        [FixedLength(0x20, StringEncoding = StringEncoding.UTF8)]
        public string bootSystemId;
        [FixedLength(0x20, StringEncoding = StringEncoding.UTF8)]
        public string bootId;
    }

    class IsoPrimaryVolumeDescriptor
    {
        public byte zero0;
        [FixedLength(0x20, StringEncoding = StringEncoding.UTF8)]
        public string systemId;
        [FixedLength(0x20, StringEncoding = StringEncoding.UTF8)]
        public string volumeId;
        public long zero1;
        public IsoUInt32 spaceSize;
        [FixedLength(0x20)]
        public byte[] zero2;

        public IsoUInt16 setSize;
        public IsoUInt16 seqCount;
        public IsoUInt16 logicalBlockSize;
        public IsoUInt32 pathTableSize;
        public IsoLbaPathTable pathTable;

        public IsoDirEntry rootDir;

        [FixedLength(0x80)]
        public string volumeSetId;
        [FixedLength(0x80)]
        public string publisherId;
        [FixedLength(0x80)]
        public string dataPreparerId;
        [FixedLength(0x80)]
        public string applicationId;
        [FixedLength(0x26)]
        public string copyrightFileId;
        [FixedLength(0x24)]
        public string abstractFileId;
        [FixedLength(0x25)]
        public string bibliographicFileId;

        public IsoDecDateTime createDateTime;
        public IsoDecDateTime modDateTime;
        public IsoDecDateTime expireDateTime;
        public IsoDecDateTime effectiveDateTime;

        public byte fileStructureVersion;
        public byte zero3;
    }

    class IsoDirEntry
    {
        public byte length;
        public IsoDirEntryBody body;

        public bool IsDirectory => (body.flags & 0x02) > 0;
    }

    [Alignment(0x2)]
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

        [VariableLength("fileNameLength", StringEncoding = StringEncoding.UTF8)]
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
        [FixedLength(4)]
        public string year;
        [FixedLength(2)]
        public string month;
        [FixedLength(2)]
        public string day;
        [FixedLength(2)]
        public string hour;
        [FixedLength(2)]
        public string minute;
        [FixedLength(2)]
        public string second;
        [FixedLength(2)]
        public string microSecond;

        public byte timeZone;
    }

    class IsoUInt32
    {
        [Endianness(ByteOrder = ByteOrder.LittleEndian)]
        public uint valueLe;
        [Endianness(ByteOrder = ByteOrder.BigEndian)]
        public uint valueBe;

        public uint Value
        {
            get => valueLe;
            set
            {
                valueLe = value;
                valueBe = value;
            }
        }
    }

    class IsoUInt16
    {
        [Endianness(ByteOrder = ByteOrder.LittleEndian)]
        public ushort valueLe;
        [Endianness(ByteOrder = ByteOrder.BigEndian)]
        public ushort valueBe;

        public ushort Value
        {
            get => valueLe;
            set
            {
                valueLe = value;
                valueBe = value;
            }
        }
    }

    class IsoLbaPathTable
    {
        [Endianness(ByteOrder = ByteOrder.LittleEndian)]
        public uint lbaPathTableLe;
        [Endianness(ByteOrder = ByteOrder.LittleEndian)]
        public uint optLbaPathTableLe;
        [Endianness(ByteOrder = ByteOrder.BigEndian)]
        public uint lbaPathTableBe;
        [Endianness(ByteOrder = ByteOrder.BigEndian)]
        public uint optLbaPathTableBe;

        public uint LbaPathTable
        {
            get => lbaPathTableLe;
            set
            {
                lbaPathTableLe = value;
                lbaPathTableBe = value;
            }
        }

        public uint OptLbaPathTable
        {
            get => optLbaPathTableLe;
            set
            {
                optLbaPathTableLe = value;
                optLbaPathTableBe = value;
            }
        }
    }

    [Endianness(ByteOrder = ByteOrder.LittleEndian)]
    [Alignment(0x2)]
    class IsoPathTableEntry
    {
        public byte dirNameLength;
        public byte extAttr;
        public uint lbaExtent;
        public ushort parentDirIndex;

        [VariableLength("dirNameLength")]
        public string dirName;
    }

    class Ps2DiscArchiveFileInfo : ArchiveFileInfo
    {
        public IsoDirEntry Entry { get; }

        public Ps2DiscArchiveFileInfo(Stream fileData, string filePath, IsoDirEntry entry) :
            base(fileData, GetFileName(filePath))
        {
            Entry = entry;
        }

        public Ps2DiscArchiveFileInfo(Stream fileData, string filePath, IKompressionConfiguration configuration, long decompressedSize) :
            base(fileData, GetFileName(filePath), configuration, decompressedSize)
        {
        }

        private static string GetFileName(string fileName)
        {
            return fileName.Split(';')[0];
        }
    }
}
