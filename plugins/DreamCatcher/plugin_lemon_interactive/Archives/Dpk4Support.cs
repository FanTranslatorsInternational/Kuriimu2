using Komponent.IO.Attributes;
#pragma warning disable 649

namespace plugin_lemon_interactive.Archives
{
    /// <summary>
    /// 
    /// </summary>
    class Dpk4Header
    {
        [FixedLength(4)]
        public string Magic = "DPK4";
        public uint fileSize;
        public int fileTableSize;
        public int fileCount;
    }

    /// <summary>
    /// 
    /// </summary>
    [Alignment(4)]
    class Dpk4FileEntry
    {
        public int entrySize;
        public int size;
        public int compressedSize;
        public int offset;
        [VariableLength("entrySize", Offset = -16)]
        public string fileName;

        public bool IsCompressed => size > compressedSize;
    }
}
