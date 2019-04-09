using Komponent.IO;
using Komponent.IO.Attributes;

namespace plugin_nintendo.NW4C
{
    /// <summary>
    /// The general file header for NW4C formats.
    /// </summary>
    public class NW4CHeader
    {
        [FixedLength(4)]
        public string Magic;
        public ByteOrder ByteOrder;
        public short HeaderSize;
        public int Version;
        public int FileSize;
        public short SectionCount;
        public short Padding;
    }
}
