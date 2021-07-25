using System.IO;
using System.Text;
using Komponent.IO;
using Komponent.IO.Attributes;
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

    class Sor4Support
    {
        public static Platform DeterminePlatform(Stream texListStream)
        {
            using var br = new BinaryReaderX(texListStream, true);

            var entry1 = br.ReadType<Sor4Entry>();
            var entry2 = br.ReadType<Sor4Entry>();

            texListStream.Position = 0;

            // Platform is determined by the alignment between the first 2 entries
            // Switch aligns all files to 16 bytes and precedes them with the decompressed size
            // Pc does not align files. It also does not precede them with the decompressed size
            if (entry1.compSize == entry2.offset)
                return Platform.Pc;

            return Platform.Switch;
        }
    }

    enum Platform
    {
        Switch,
        Pc
    }
}
