using Komponent.IO;

namespace plugin_alpha_dream.Archives
{
    struct Bg4Header
    {
        public string magic; // BG4\0
        public short version; // 0x105
        public short fileEntryCount;
        public int metaSecSize;
        public short fileEntryCountDerived;
        public short fileEntryCountMultiplier;  // fileEntryCountDerived * fileEntryCountMultiplier = fileEntryCount
    }

    struct Bg4Entry
    {
        public uint fileOffset;
        public uint fileSize;    // MSB is set if file is compressed
        public uint nameHash;
        public short nameOffset;

        public int FileOffset
        {
            get => (int)(fileOffset & 0x7FFFFFFF);
            set => fileOffset = (fileOffset & 0x80000000) | (uint)value;
        }

        public int FileSize
        {
            get => (int)(fileSize & 0x7FFFFFFF);
            set => fileSize = (fileSize & 0x80000000) | (uint)value;
        }

        public bool IsInvalid => (fileSize & 0x80000000) > 0;

        public bool IsCompressed
        {
            get => (fileOffset & 0x80000000) > 0;
            set => fileOffset = (fileOffset & 0x7FFFFFFF) | (value ? 0x80000000 : 0);
        }
    }

    class Bg4Support
    {
        public static long PeekDecompressedSize(Stream input)
        {
            var bkPos = input.Position;

            using var br = new BinaryReaderX(input, true);
            input.Position = input.Length - 4;

            var decompressedSize = br.ReadInt32() + input.Length;
            input.Position = bkPos;

            return decompressedSize;
        }
    }
}
