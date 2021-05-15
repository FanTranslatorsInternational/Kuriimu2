using Komponent.IO.Attributes;
using Kontract.Models.IO;

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
        public ByteOrder byteOrder;
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
