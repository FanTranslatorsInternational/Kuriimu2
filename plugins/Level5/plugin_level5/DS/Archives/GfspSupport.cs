using Komponent.IO.Attributes;

namespace plugin_level5.DS.Archives
{
    class GfspHeader
    {
        [FixedLength(4)]
        public string magic = "GFSP";

        public byte fc1;
        public byte fc2;

        public ushort infoOffsetUnshifted;
        public ushort nameTableOffsetUnshifted;
        public ushort dataOffsetUnshifted;
        public ushort infoSizeUnshifted;
        public ushort nameTableSizeUnshifted;
        public uint dataSizeUnshifted;

        public ushort FileCount
        {
            get => (ushort)((fc2 & 0xf) << 8 | fc1);
            set
            {
                fc2 = (byte)((fc2 & 0xF0) | ((value >> 8) & 0x0F));
                fc1 = (byte)value;
            }
        }

        public ushort FileInfoOffset
        {
            get => (ushort)(infoOffsetUnshifted << 2);
            set => infoOffsetUnshifted = (ushort)(value >> 2);
        }

        public ushort FilenameTableOffset
        {
            get => (ushort)(nameTableOffsetUnshifted << 2);
            set => nameTableOffsetUnshifted = (ushort)(value >> 2);
        }

        public ushort DataOffset
        {
            get => (ushort)(dataOffsetUnshifted << 2);
            set => dataOffsetUnshifted = (ushort)(value >> 2);
        }

        public ushort FileInfoSize
        {
            get => (ushort)(infoSizeUnshifted << 2);
            set => infoSizeUnshifted = (ushort)(value >> 2);
        }

        public ushort FilenameTableSize
        {
            get => (ushort)(nameTableSizeUnshifted << 2);
            set => nameTableSizeUnshifted = (ushort)(value >> 2);
        }

        public uint DataSize
        {
            get => dataSizeUnshifted << 2;
            set => dataSizeUnshifted = value >> 2;
        }
    }

    class GfspFileInfo
    {
        public ushort hash;
        public ushort tmp;
        public ushort size;
        public ushort tmp2;

        public int NameOffset
        {
            get => tmp2 >> 4;
            set => tmp2 = (ushort)(value << 4);
        }

        public int FileOffset
        {
            get => tmp << 2;
            set => tmp = (ushort)(value >> 2);
        }
    }
}
