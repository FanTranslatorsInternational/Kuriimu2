using Komponent.IO.Attributes;
using Kontract.Models.IO;
#pragma warning disable 649

namespace plugin_nintendo.Archives
{
    class NarcHeader
    {
        [FixedLength(4)]
        public string magic = "NARC";
        public ByteOrder bom = ByteOrder.LittleEndian;
        public short version = 0x100;
        public int fileSize;
        public short chunkSize = 0x10;
        public short chunkCount = 0x3;
    }

    class NarcFatHeader
    {
        [FixedLength(4)]
        public string magic = "BTAF";
        public int chunkSize;
        public short fileCount;
        public short reserved1;
    }

    class NarcFntHeader
    {
        [FixedLength(4)]
        public string magic = "BTNF";
        public int chunkSize;
    }
}
