namespace plugin_ruby_party.Archives
{
    class CdarHeader
    {
        public string magic = "CDAR";
        public int unk1;
        public int entryCount;
        public int unk2;
    }

    class CdarFileEntry
    {
        public int offset;
        public int decompSize;
        public int size;
    }
}
