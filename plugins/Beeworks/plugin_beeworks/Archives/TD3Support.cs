namespace plugin_beeworks.Archives
{
    class TD3Header
    {
        public int fileCount;
        public int nameBufSize = 0x40;
    }

    class TD3Entry
    {
        public int offset;
        public int size;
        public string fileName;
    }
}
