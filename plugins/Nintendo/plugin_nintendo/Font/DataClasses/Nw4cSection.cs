namespace plugin_nintendo.Font.DataClasses
{
    struct Nw4cSection
    {
        public long sectionOffset;

        public string magic;
        public int sectionSize;
        public object sectionData;
    }
}
