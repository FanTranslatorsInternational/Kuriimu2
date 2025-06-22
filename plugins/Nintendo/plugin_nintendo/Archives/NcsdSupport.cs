namespace plugin_nintendo.Archives
{
    class NcsdHeader
    {
        public byte[] rsa2048;
        public string magic;
        public int ncsdSize;
        public long mediaId;
        public byte[] partitionFsType;
        public byte[] partitionCryptType;
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
        public byte[] exHeaderHash;
        public int additionalHeaderSize;
        public int sectorZeroOffset;
        public byte[] partitionFlags;
        public long[] partitionIds;
        public byte[] reserved1;
        public byte[] reserved2;
        public byte unk1;
        public byte unk2;

        public NcsdCardInfoHeader cardInfoHeader;
    }

    class NcsdCardInfoHeader
    {
        public int card2WriteAddress;   // in mediaUnits
        public int cardBitMask;
        public byte[] reserved1;
        public short titleVersion;
        public short cardRevision;
        public byte[] reserved2;
        public byte[] cardSeedKeyY;
        public byte[] encryptedCardSeed;
        public byte[] cardSeedAesMac;
        public byte[] cardSeedNonce;
        public byte[] reserved3;
        public byte[] firstNcchHeader;
    }
}
