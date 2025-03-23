namespace plugin_level5.Common.Font.Models
{
    public struct Fnt00Header
    {
        public string magic;
        public int version;
        public short largeCharHeight;
        public short smallCharHeight;
        public ushort largeEscapeCharacterIndex;
        public ushort smallEscapeCharacterIndex;
        public long zero0;

        public short charSizeOffset;
        public short charSizeCount;
        public short largeCharSjisOffset;
        public short largeCharSjisCount;
        public short smallCharSjisOffset;
        public short smallCharSjisCount;
        public short largeCharUnicodeOffset;
        public short largeCharUnicodeCount;
        public short smallCharUnicodeOffset;
        public short smallCharUnicodeCount;
    }
}
