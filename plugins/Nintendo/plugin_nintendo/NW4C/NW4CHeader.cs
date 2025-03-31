using Komponent.Contract.Aspects;
using Komponent.Contract.Enums;

namespace plugin_nintendo.NW4C
{
    /// <summary>
    /// The general file header for NW4C formats.
    /// </summary>
    class NW4CHeader
    {
        [FixedLength(4)]
        public string magic;
        [Endianness(ByteOrder = ByteOrder.BigEndian)]
        public ushort byteOrder;
        public short headerSize;
        public int version;
        public int fileSize;
        public short sectionCount;
        public short padding;
    }

    class NW4CSection<TSection>
    {
        [FixedLength(4)]
        public string magic;
        public int sectionSize;
        public TSection sectionData;
    }
}
