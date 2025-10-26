namespace plugin_nintendo.Font.DataClasses.Common
{
    struct CwdhSection
    {
        public short startIndex;
        public short endIndex;
        public int nextCwdhOffset;
        public CwdhEntry[] entries;
    }
}
