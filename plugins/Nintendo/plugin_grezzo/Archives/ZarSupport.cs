using Komponent.Contract.Aspects;

namespace plugin_grezzo.Archives
{
    struct ZarHeader
    {
        [FixedLength(3)] 
        public string magic; // ZAR
        public byte version; // 1

        public int fileSize;
        public short fileTypeCount;
        public short fileCount;

        public int fileTypeEntryOffset;
        public int fileEntryOffset;
        public int fileOffsetsOffset;

        [FixedLength(8)]
        public string headerString;
    }

    struct ZarFileTypeEntry
    {
        public int fileCount;
        public int fileIndexOffset;
        public int fileTypeNameOffset;
        public int unk1; // -1
    }

    struct ZarFileEntry
    {
        public int fileSize;
        public int fileNameOffset;
    }
}
