using Komponent.Contract.Aspects;

namespace plugin_nintendo.NW4C
{
    /// <summary>
    /// The general file header for NW4C formats.
    /// </summary>
    struct NW4CHeader
    {
        public string magic;
        public ushort byteOrder;
        public short headerSize;
        public int version;
        public int fileSize;
        public short sectionCount;
        public short padding;
    }

    struct NW4CSection<TSection>
    {
        public string magic;
        public int sectionSize;
        public TSection sectionData;
    }
}
