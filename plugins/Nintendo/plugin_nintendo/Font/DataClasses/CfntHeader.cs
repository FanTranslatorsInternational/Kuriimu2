namespace plugin_nintendo.Font.DataClasses
{
    struct CfntHeader
    {
        public string magic;
        public ushort endianess;
        public ushort headerSize;
        public int version;
        public int fileSize;
        public int blockCount;
    }
}
