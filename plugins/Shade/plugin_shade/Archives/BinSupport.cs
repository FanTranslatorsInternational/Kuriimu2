using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Kontract.Interfaces.Progress;
using Kontract.Kompression.Configuration;
using Kontract.Models.Archive;
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
        public int offSize;
    }

    class BinArchiveFileInfo : ArchiveFileInfo
    {
        public BinFileInfo Entry { get; }

        public long OriginalSize { get; }

        public BinArchiveFileInfo(Stream fileData, string filePath, BinFileInfo entry) :
            base(fileData, filePath) 
        {
            Entry = entry;
            OriginalSize = fileData.Length;
        }
        public BinArchiveFileInfo(Stream fileData, string filePath, BinFileInfo entry, IKompressionConfiguration configuration, long decompressedSize) : 
            base(fileData, filePath, configuration, decompressedSize)
        {
            Entry = entry;
            OriginalSize = fileData.Length;
        }
    }
}
