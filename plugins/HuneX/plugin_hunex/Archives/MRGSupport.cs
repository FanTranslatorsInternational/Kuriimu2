namespace plugin_hunex.Archives
{
    class MRGHeader
    {
        public string magic = "mrgd00";
        public short fileCount;
    }

    class MRGEntry
    {
        public ushort sectorOffset;
        public ushort lowOffset;
        public ushort sectorCount;
        public ushort lowSize;

        public int Offset
        {
            get => sectorOffset * 0x800 + lowOffset;
            set
            {
                sectorOffset = (ushort)(value / 0x800);
                lowOffset = (ushort)(value % 0x800);
            }
        }

        // HINT: Some entries seem to not correlate with the sectorCount
        // Sometimes the sectorCount is less than a block above the lowSize, sometimes less than a block below lowSize
        public int Size
        {
            get => (sectorCount - 1) / 0x20 * 0x800 * 0x20 + lowSize;
            set
            {
                lowSize = (ushort)value;
                sectorCount = (ushort)(((value + 0x7FF) & ~0x7FF) / 0x800);
            }
        }
    }
}
