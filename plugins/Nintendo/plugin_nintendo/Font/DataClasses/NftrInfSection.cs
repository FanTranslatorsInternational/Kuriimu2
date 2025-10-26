using plugin_nintendo.Font.DataClasses.Common;

namespace plugin_nintendo.Font.DataClasses
{
    struct NftrInfSection
    {
        public byte fontType;
        public byte lineFeed;
        public ushort fallbackCharIndex;
        public CwdhEntry defaultWidths;
        public byte encoding;
        public int cglpOffset;
        public int cwdhOffset;
        public int cmapOffset;

        public byte height;
        public byte width;
        public byte bearingX;
        public byte bearingY;

        public bool hasExtendedData;
    }
}
