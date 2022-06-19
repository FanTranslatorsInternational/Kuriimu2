using System.IO;
using Kontract.Kompression.Configuration;
using Kontract.Models.Archive;
#pragma warning disable 649

namespace plugin_shade.Archives
{
    class Mcb0Entry
    {
        public short id;
        public short unk2;
        public uint offset;
        public uint size;
    }

    class BlnArchiveFileInfo : ArchiveFileInfo
    {
        public Mcb0Entry Entry { get; }

        public BlnArchiveFileInfo(Stream fileData, string filePath, Mcb0Entry entry) :
            base(fileData, filePath)
        {
            Entry = entry;
        }

        public BlnArchiveFileInfo(Stream fileData, string filePath, Mcb0Entry entry, IKompressionConfiguration configuration, long decompressedSize) :
            base(fileData, filePath, configuration, decompressedSize)
        {
            Entry = entry;
        }
    }
}
