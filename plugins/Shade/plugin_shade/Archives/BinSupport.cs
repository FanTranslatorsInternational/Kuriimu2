using System.IO;
using Kontract.Kompression.Configuration;
#pragma warning disable 649

namespace plugin_shade.Archives
{
    class BinHeader
    {
        public int fileCount;
        public int padFactor;
        public int mulFactor;
        public int shiftFactor;
        public int mask;
    }
    
    class BinFileInfo
    {
        public uint offSize;
    }

    class BinArchiveFileInfo : ShadeArchiveFileInfo
    {
        public BinFileInfo Entry { get; }

        public BinArchiveFileInfo(Stream fileData, string filePath, BinFileInfo entry) :
            base(fileData, filePath) 
        {
            Entry = entry;
        }
        public BinArchiveFileInfo(Stream fileData, string filePath, BinFileInfo entry, IKompressionConfiguration configuration, long decompressedSize) : 
            base(fileData, filePath, configuration, decompressedSize)
        {
            Entry = entry;
        }
    }
}
