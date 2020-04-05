using System.Collections.Generic;
using System.IO;
using Kontract.Kompression.Configuration;
using Kontract.Models.Archive;

namespace plugin_level5.Archives
{
    class PckFileInfo
    {
        public uint hash;
        public int fileOffset;
        public int fileLength;
    }

    class PckArchiveFileInfo : ArchiveFileInfo
    {
        public PckFileInfo Entry { get; }

        public IList<uint> Hashes { get; }

        public PckArchiveFileInfo(Stream fileData, string filePath,PckFileInfo entry,IList<uint> hashBlock) : 
            base(fileData, filePath)
        {
            Entry = entry;
            Hashes = hashBlock;
        }

        public PckArchiveFileInfo(Stream fileData, string filePath, IKompressionConfiguration configuration, long decompressedSize) : 
            base(fileData, filePath, configuration, decompressedSize)
        {
        }
    }
}
