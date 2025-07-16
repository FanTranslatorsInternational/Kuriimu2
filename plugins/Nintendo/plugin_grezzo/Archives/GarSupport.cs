namespace plugin_grezzo.Archives
{
    struct GarHeader
    {
        public string magic; // GAR
        public byte version;
        public uint fileSize;

        public short fileTypeCount;
        public short fileCount;

        public int fileTypeEntryOffset;
        public int fileEntryOffset;
        public int fileOffsetsOffset;

        public string hold0; // jenkins
    }

    struct Gar2FileTypeEntry
    {
        public int fileCount;
        public int fileIndexOffset;
        public int fileTypeNameOffset;
        public int unk1; // -1
    }

    class Gar5FileTypeEntry
    {
        public int fileCount;
        public int unk1;
        public int fileEntryIndex;
        public int fileTypeNameOffset;
        public int fileTypeInfoOffset;
    }

    struct Gar5FileTypeInfo
    {
        public int unk1;
        public int unk2;
        public short unk3;
        public short unk4;
    }

    struct Gar2FileEntry
    {
        public uint fileSize;
        public int nameOffset;
        public int fileNameOffset;
    }

    class Gar5FileEntry
    {
        public int fileSize;
        public int fileOffset;
        public int fileNameOffset;
        public int unk1; // -1
    }
}
