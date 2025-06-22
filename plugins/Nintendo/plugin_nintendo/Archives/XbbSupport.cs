using Komponent.Contract.Aspects;

namespace plugin_nintendo.Archives
{
    [Alignment(0x20)]
    struct XbbHeader
    {
        public string magic; // XBB
        public byte version; // 1
        public int entryCount;
    }

    struct XbbFileEntry
    {
        public int offset;
        public int size;
        public int nameOffset;
        public uint hash;
    }

    struct XbbHashEntry
    {
        public uint hash;
        public int index;
    }
}
