namespace plugin_level5.Common.Archive.Models
{
    public struct XfspHeader
    {
        public string magic;
        public ushort fileCountAndType;
        public ushort infoOffset;
        public ushort nameTableOffset;
        public ushort dataOffset;
        public ushort infoSize;
        public ushort nameTableSize;
        public uint dataSize;
    }
}
