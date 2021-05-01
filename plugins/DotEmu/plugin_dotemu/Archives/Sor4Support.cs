using System.IO;
using System.Text;
using Komponent.IO;
using Kontract.Kompression.Configuration;
using Kontract.Models.Archive;

namespace plugin_dotemu.Archives
{
    class Sor4Entry
    {
        public string path;

        public int offset;
        public int flags;
        public int compSize;
    }

    class Sor4ArchiveFileInfo : ArchiveFileInfo
    {
        public Sor4Entry Entry { get; }

        public Sor4ArchiveFileInfo(Stream fileData, string filePath, Sor4Entry entry, IKompressionConfiguration configuration, long decompressedSize) : base(fileData, filePath, configuration, decompressedSize)
        {
            Entry = entry;
        }
    }
}
