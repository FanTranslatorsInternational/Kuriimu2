using Komponent.IO.Attributes;
#pragma warning disable 649

namespace plugin_nintendo.Archives
{
    class SbHeader
    {
        [FixedLength(2)] 
        public string magic = "SB";
        public short entryCount;
    }
}
