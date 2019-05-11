using Komponent.IO.Attributes;

namespace plugin_test_adapters.Archive.Models
{
    [Alignment(0x10)]
    class Header
    {
        [FixedLength(8)]
        public string magic="ARC TEST";
        public int fileCount;
    }
}
