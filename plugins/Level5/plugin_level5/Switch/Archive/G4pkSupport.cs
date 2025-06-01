namespace plugin_level5.Switch.Archive
{
    struct G4pkHeader
    {
        public string magic; // G4PK
        public short headerSize; // 0x40
        public short fileType; // 0x64
        public int version; // 0x00100000
        public int contentSize;

        public byte[] zeroes1; // 0x10

        public int fileCount;
        public short table2EntryCount;
        public short table3EntryCount;
        public short unk2;
        public short unk3;

        public byte[] zeroes2; // 0x14
    }
}
