namespace plugin_nintendo.Font.DataClasses
{
    struct CfntInfSection
    {
        public byte fontType;
        public byte lineFeed;
        public ushort fallbackCharIndex;
        public CfntCwdhEntry defaultWidths;
        public byte encoding;
        public int tglpOffset;
        public int cwdhOffset;
        public int cmapOffset;
        public byte height;
        public byte width;
        public byte ascent;
        public byte reserved;
    }
}
