using Komponent.IO.Attributes;

namespace plugin_sting_entertainment.Archives
{
    class PckHeader
    {
        [FixedLength(8)]
        public string magic;
        public int size;
    }

    class PckEntry
    {
        public int offset;
        public int size;
    }

    class PckSupport
    {
    }
}
