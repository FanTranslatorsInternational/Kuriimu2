using Komponent.IO.Attributes;
#pragma warning disable 649

namespace plugin_square_enix.Archives
{
    class DpkHeader
    {
        public int fileCount;
        public int fileSize;
    }

    [Alignment(0x80)]
    class DpkEntry
    {
        [FixedLength(0x16)]
        public string name;
        public short nameSum;
        public int offset;
        public int compSize;
        public int decompSize;
    }
}
