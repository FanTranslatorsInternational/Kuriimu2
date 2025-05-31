namespace plugin_nintendo.Font.DataClasses
{
    struct CfntCwdhSection
    {
        public short startIndex;
        public short endIndex;
        public int nextCwdhOffset;
        public CfntCwdhEntry[] entries;
    }
}
