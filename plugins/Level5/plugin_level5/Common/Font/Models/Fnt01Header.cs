namespace plugin_level5.Common.Font.Models
{
    public struct Fnt01Header
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
        public short largeCharOffset;
        public short largeCharCount;
        public short smallCharOffset;
        public short smallCharCount;
    }
}
