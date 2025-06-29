namespace plugin_criware.Archives
{
    class CvmHeader
    {
        public string magic = "CVMH";
        public long headerSize;
        public byte[] reserved1;
        public long fileSize;
        public byte[] date;
        public byte padding1;
        public int version1;
        public int flags;   // 0x10 if encrypted
        public string rofsMagic="ROFS";
        public string makeToolId;
        public int version2;
        public byte unk1;
        public byte unk2;
        public short unk3;
        public int sectorCount;
        public int zoneSector;
        public int isoSectorStart;
        public byte[] padding;
        public int[] sectorCounts;

        public bool IsEncrypted => (flags & 0x10) > 0;
    }

    class CvmZoneInfo
    { 
        public string magic;
        public int unk1;
        public int unk2;
        public int unk3;
        public byte[] unk4;

        public int sectorLength1;
        public int sectorLength2;
        public CvmZoneDataLoc dataLoc1;
        public CvmZoneDataLoc isoDataLoc;
    }

    class CvmZoneDataLoc
    {
        public int sectorIndex;
        public long length;
    }

    class IsoPrimaryDescriptor
    {
        public byte type;
        public string id;
        public byte version;
        public byte unused1;
        public string system_id;
        public string volume_id;
        public byte[] unused2;
        public int volSizeLe;
        public int volSizeBe;
        public byte[] escapeSequences;
        public int volSetSize;
        public int volSequenceNumber;
        public short logicalBlockSizeLe;
        public short logicalBlockSizeBe;
        public int pathTableSizeLe;
        public int pathTableSizeBe;
        public int typelPathTable;
        public int optTypelPathTable;
        public int typemPathTable;
        public int optTypemPathTable;
        public IsoDirectoryRecord rootDirRecord;
        public string volumeSetId;
        public string publisherId;
        public string preparerId;
        public string applicationId;
        public string copyrightFileId;
        public string abstractFileId;
        public string bibliographicFileId;
        public string creationDate;
        public string modificationDate;
        public string expirationDate;
        public string effectiveDate;
        public byte fileStructureVersion;
        public byte unused4;
        public byte[] applicationData;
    }

    class IsoDirectoryRecord
    {
        public byte length;
        public byte extAttributeLength;
        public uint extentLe;
        public uint extentBe;
        public uint sizeLe;
        public uint sizeBe;
        public byte[] date;
        public byte flags;
        public byte fileUnitSize;
        public byte interleave;
        public int volumeSequenceNumber;
        public byte nameLength;
        public string name;
    }
}
