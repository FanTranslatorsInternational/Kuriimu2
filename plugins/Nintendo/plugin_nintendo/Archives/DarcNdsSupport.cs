using Komponent.IO.Attributes;

namespace plugin_nintendo.Archives
{
    class DarcNdsHeader
    {
        [FixedLength(4)] 
        public string magic = "DARC";
        public int fileCount;
    }
}
