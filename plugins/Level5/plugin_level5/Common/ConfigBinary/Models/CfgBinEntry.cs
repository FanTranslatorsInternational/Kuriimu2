namespace plugin_level5.Common.ConfigBinary.Models
{
    internal struct CfgBinEntry
    {
        public uint crc32;
        public byte entryCount;
        public byte[] entryTypes;
        public int[] entryValues;
    }
}
