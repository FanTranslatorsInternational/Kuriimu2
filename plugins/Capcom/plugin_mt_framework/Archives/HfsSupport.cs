using Komponent.IO.Attributes;

namespace plugin_mt_framework.Archives
{
    [Alignment(0x10)]
    class HfsHeader
    {
        [FixedLength(4)]
        public string magic;
        public short version;
        public short type;
        public int fileSize;
    }
}
