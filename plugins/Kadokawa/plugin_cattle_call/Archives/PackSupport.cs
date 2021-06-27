using System.IO;
using Kontract.Kompression.Configuration;
using Kontract.Models.Archive;

namespace plugin_cattle_call.Archives
{
    class PackEntry
    {
        public uint hash;
        public int offset;
        public int size;
    }

    class PackArchiveFileInfo : ArchiveFileInfo
    {
        public PackEntry Entry { get; }

        public PackArchiveFileInfo(Stream fileData, string filePath, PackEntry entry) : base(fileData, filePath)
        {
            Entry = entry;
        }

        public PackArchiveFileInfo(Stream fileData, string filePath, PackEntry entry, IKompressionConfiguration configuration, long decompressedSize) : base(fileData, filePath, configuration, decompressedSize)
        {
            Entry = entry;
        }
    }
}
