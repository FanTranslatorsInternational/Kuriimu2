namespace plugin_nintendo.Font.DataClasses
{
    struct NftrHeader
    {
        public string magic;
        public ushort endianess;
        public ushort version;
        public int fileSize;
        public short infoOffset;
        public short blockCount;
    }
}
