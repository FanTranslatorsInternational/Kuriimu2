using Komponent.IO;

namespace plugin_space_channel_5.TEXT
{
    public sealed class TEXTHeader
    {
        [FixedLength(4)]
        public string Magic = "TEXT";
        public int FileSize;
        public int EntryCount;
        public int Padding;
    }

    public sealed class TextGroupMetadata
    {
        public int NameOffset;
        public int EntryCount;
        public int EntryOffset;
        public int Padding;
    }
}
