using Komponent.IO.Attributes;

namespace plugin_nintendo.Archives
{
    class SbHeader
    {
        [FixedLength(2)] 
        public string magic = "SB";
        public short entryCount;
    }
}
