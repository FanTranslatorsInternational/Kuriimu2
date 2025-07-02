namespace plugin_spike_chunsoft.Archives
{
    class NonaryHeader
    {
        public string magic;
        public int hashTableOffset;
        public int fileEntryOffset;
        public long dataOffset;
        public long infoSecSize;
        public int hold0;
    }

    class NonaryTableHeader
    {
        public int tableSize;
        public int entryCount;
        public long hold0;
    }

    class NonaryDirectoryEntry
    {
        public uint directoryHash;
        public int fileCount;
        public int unk1;
        public uint hold0;
    }

    class NonaryEntry
    {
        public long fileOffset;
        public byte[] XorPad;
        public long fileSize;
        public uint XorId;
        public short directoryHashId;
        public short const0;
        public uint hold0;
    }

    class NonarySupport
    {
        public static uint Hash999(string s) => (uint)(s.Aggregate(0, (n, c) => n * 131 + (c & ~32)) * 16 | s.Sum(c => c) % 16) * 2 / 2;
    }
}
