using Komponent.Contract.Aspects;

namespace plugin_nintendo.Archives
{
    class PcHeader
    {
        [FixedLength(2)]
        public string magic = "PC";
        public short entryCount;
    }
}
