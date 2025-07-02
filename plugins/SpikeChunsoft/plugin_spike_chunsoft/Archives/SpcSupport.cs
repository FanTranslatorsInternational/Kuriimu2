namespace plugin_spike_chunsoft.Archives
{
    class SpcHeader
    {
        public string magic = "CPS.";
        public int zero0;
        public long unk1 = -1;
    }

    class SpcEntry
    {
        public short flag;
        public short unk1 = 4;
        public int compSize;
        public int decompSize;
        public int nameLength;
        public byte[] zero0 = new byte[0x10];
        public string name;
    }
}
