using Komponent.IO.Attributes;

namespace plugin_level5.Archives
{
    class Lpc2Header
    {
        [FixedLength(4)] 
        public string magic = "LPC2";
        public int fileCount;
        public int headerSize;
        public int fileSize;

        public int fileEntryOffset;
        public int nameOffset;
        public int dataOffset;
    }

    class Lpc2FileEntry
    {
        public int nameOffset;
        public int fileOffset;
        public int fileSize;
    }
}
