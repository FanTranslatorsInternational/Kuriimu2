using Komponent.IO.Attributes;
#pragma warning disable 649

namespace plugin_nintendo.Archives
{
    class NcsdHeader
    {
        [FixedLength(0x100)] 
        public byte[] rsa2048;
        [FixedLength(4)] 
        public string magic;
        public int ncsdSize;
        public long mediaId;
        [FixedLength(8)]
        public byte[] partitionFsType;
        [FixedLength(8)]
        public byte[] partitionCryptType;
        [FixedLength(8)] 
        public NcsdPartitionEntry[] partitionEntries;

        // This could also be a header for NAND, but we're only interested in card ridges
        public NcsdCardHeader cardHeader;
    }

    class NcsdPartitionEntry
    {
        public int offset;
        public int length;
    }

    class NcsdCardHeader
    {
        [FixedLength(0x20)] 
        public byte[] exHeaderHash;
        public int additionalHeaderSize;
        public int sectorZeroOffset;
        [FixedLength(8)] 
        public byte[] partitionFlags;
        [FixedLength(8)] 
        public long[] partitionIds;
        [FixedLength(0x20)] 
        public byte[] reserved1;
        [FixedLength(0xE)] 
        public byte[] reserved2;
        public byte unk1;
        public byte unk2;

        public NcsdCardInfoHeader cardInfoHeader;
    }

    class NcsdCardInfoHeader
    {
        public int card2WriteAddress;   // in mediaUnits
        public int cardBitMask;
        [FixedLength(0x108)]
        public byte[] reserved1;
        public short titleVersion;
        public short cardRevision;
        [FixedLength(0xCEC)] 
        public byte[] reserved2;
        [FixedLength(0x10)] 
        public byte[] cardSeedKeyY;
        [FixedLength(0x10)] 
        public byte[] encryptedCardSeed;
        [FixedLength(0x10)] 
        public byte[] cardSeedAesMac;
        [FixedLength(0xC)] 
        public byte[] cardSeedNonce;
        [FixedLength(0xC4)] 
        public byte[] reserved3;
        [FixedLength(0x100)] 
        public byte[] firstNcchHeader;
    }
}
