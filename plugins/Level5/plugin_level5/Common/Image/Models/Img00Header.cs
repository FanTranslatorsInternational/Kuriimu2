namespace plugin_level5.Common.Image.Models
{
    public struct Img00Header
    {
        public string magic;
        public short entryOffset;
        public byte imageFormat;
        public byte const1;
        public byte imageCount;
        public byte bitDepth;
        public short bytesPerTile;
        public short width;
        public short height;
        public ushort paletteInfoOffset;
        public ushort paletteInfoCount;
        public ushort imageInfoOffset;
        public ushort imageInfoCount;
        public int dataOffset;
        public int platform;
    }
}
