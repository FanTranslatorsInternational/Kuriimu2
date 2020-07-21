using Komponent.IO.Attributes;

namespace plugin_level5.Switch.Archives
{
    class G4pkHeader
    {
        [FixedLength(4)]
        public string magic = "G4PK";
        public short headerSize = 0x40;
        public short fileType = 0x64;
        public int version = 0x00100000;
        public int contentSize;

        [FixedLength(0x10)]
        public byte[] zeroes1 = new byte[0x10];

        public int fileCount;
        public short table2EntryCount;
        public short table3EntryCount;
        public short unk2;
        public short unk3;

        [FixedLength(0x14)]
        public byte[] zeroes2 = new byte[0x14];
    }
}
