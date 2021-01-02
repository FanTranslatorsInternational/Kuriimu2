using Komponent.IO.Attributes;
using Kontract.Models.IO;

namespace plugin_nintendo.NW4C
{
    /// <summary>
    /// The general file header for NW4C formats.
    /// </summary>
    public class NW4CHeader
    {
        [FixedLength(4)]
        public string Magic;
        [Endianness(ByteOrder = ByteOrder.BigEndian)]
        public ByteOrder ByteOrder;
        public short HeaderSize;
        public int Version;
        public int FileSize;
        public short SectionCount;
        public short Padding;
    }
}
