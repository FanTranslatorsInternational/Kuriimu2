namespace plugin_hunex.Archives
{
    #region Entry

    interface IHedEntry
    {
        int Offset { get; }
        int Size { get; }
    }

    class HedEntry1 : IHedEntry
    {
        public ushort lowOffset;
        public ushort highOffset;
        public ushort sectorCount;
        public ushort lowSize;

        public int Offset => (((highOffset & 0xF000) << 4) | lowOffset) * 0x800;
        public int Size => (int)(lowSize == 0 ? sectorCount * 0x800 : (((sectorCount - 1) & 0xFFFF0000) * 0x800) | lowSize);
    }

    class HedEntry2 : IHedEntry
    {
        public ushort lowOffset;
        public ushort offsetSize;

        public int Offset => (((offsetSize & 0xF000) << 4) | lowOffset) * 0x800;
        public int Size => (offsetSize & 0xFFF) * 0x800;
    }

    #endregion

    #region Nam

    interface INamEntry
    {
        string Name { get; }
    }

    class NamEntry1 : INamEntry
    {
        public string name;

        public string Name => name.Replace("\r\n", "").Trim('\0');
    }

    class NamEntry2 : INamEntry
    {
        public string name;

        public string Name => name.Trim('\0');
    }

    #endregion
}
