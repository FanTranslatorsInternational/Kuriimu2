using Komponent.IO.Attributes;

namespace plugin_sonic_generations.Text
{
    class SharpMsgHeader
    {
        [FixedLength(4)]
        public string magic;
        public uint const1;
        public int entryCount;
        public int entryTableOffset;
    }

    class SharpMsgEntryInfo
    {
        public int const1; // always 1?
        public int dataOffset;
        public int labelOffset;
        public int zero;
    }

    class SharpMsgEntryDataInfo
    {
        public int const1; // always 1?
        public int ppString;
        public ulong zero1;
        public int pString;
        [FixedLength(0xC)]
        public byte[] padding;
    }

    class SharpMsgEntry
    {
        public SharpMsgEntryInfo entryInfo;
        public SharpMsgEntryDataInfo entryDataInfo;
        public string message;
        public string label;
    }
}
