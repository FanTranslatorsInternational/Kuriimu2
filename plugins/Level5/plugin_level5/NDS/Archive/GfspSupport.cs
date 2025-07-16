namespace plugin_level5.NDS.Archive
{
    struct GfspHeader
    {
        public string magic; // GFSP

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
            get => (ushort)((fc2 & 0x0F) << 8 | fc1);
            set
            {
                fc2 = (byte)((fc2 & 0xF0) | ((value >> 8) & 0x0F));
                fc1 = (byte)value;
            }
        }

        public int ArchiveType
        {
            get => fc2 >> 4;
            set => fc2 = (byte)((value << 4) | (fc2 & 0xF));
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

    struct GfspFileInfo
    {
        public ushort hash;
        public ushort tmp;
        public ushort size;
        public ushort tmp2;

        public int NameOffset
        {
            get => tmp2 >> 4;
            set => tmp2 = (ushort)((tmp2 & 0xF) | (value << 4));
        }

        public int FileOffset
        {
            get => tmp << 2;
            set => tmp = (ushort)(value >> 2);
        }

        public int FileSize
        {
            get => size | (((tmp2 >> 2) & 0x3) << 16);
            set
            {
                size = (ushort)value;
                tmp2 = (ushort)((tmp2 & 0xFFF3) | ((value >> 16) << 2));
            }
        }
    }
}
