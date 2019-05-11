using Komponent.IO.Attributes;

namespace plugin_test_adapters.Archive.Models
{
    [Alignment(0x10)]
    class FileEntry
    {
        public int offset;
        public int size;
        public int nameLength;
        [VariableLength("nameLength",StringEncoding=StringEncoding.UTF8)]
        public string name;
    }
}
