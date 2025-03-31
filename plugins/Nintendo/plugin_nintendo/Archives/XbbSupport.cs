using Komponent.Contract.Aspects;

namespace plugin_nintendo.Archives
{
    [Alignment(0x20)]
    class XbbHeader
    {
        [FixedLength(3)]
        public string magic = "XBB";
        public byte version = 1;
        public int entryCount;
    }

    class XbbFileEntry
    {
        public int offset;
        public int size;
        public int nameOffset;
        public uint hash;
    }

    class XbbHashEntry
    {
        public uint hash;
        public int index;
    }
}
