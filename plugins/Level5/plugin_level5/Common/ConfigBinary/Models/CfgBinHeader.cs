namespace plugin_level5.Common.ConfigBinary.Models
{
    public struct CfgBinHeader
    {
        public uint entryCount;
        public uint stringDataOffset;
        public uint stringDataLength;
        public uint stringDataCount;
    }
}
