using Komponent.Contract.Aspects;

namespace plugin_nintendo.Nitro
{
    class NitroHeader
    {
        [FixedLength(4)]
        public string magic;
        public ushort byteOrder;
        public short unk1;
        public int sectionSize;
        public short headerSize;
        public short sectionCount;
    }
}
