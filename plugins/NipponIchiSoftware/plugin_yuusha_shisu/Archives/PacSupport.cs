namespace plugin_yuusha_shisu.Archives
{
    public class FileHeader
    {
        public string Magic;
        public int Unk1;
        public int FileCount;
        public int Null1;
        public string ArchiveName;
    }

    public class FileEntry
    {
        public string Extension;
        public short Unk1;
        public short FileNumbers;
        public int Checksum;
        public short Unk2;
        public short StringLength;
        public int Null2;
        public string FileName;
    }
}
