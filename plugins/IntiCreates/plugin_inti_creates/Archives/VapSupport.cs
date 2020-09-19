using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Kontract.Interfaces.Progress;
using Kontract.Kompression.Configuration;
using Kontract.Models.Archive;

namespace plugin_inti_creates.Archives
{
    class VapHeader
    {
        public int fileCount;
        public int unk1;
        public int zero0;
    }

    class VapFileEntry
    {
        public int offset;
        public int size;
        public int unk1;
        public int unk2;
    }

    class VapArchiveFileInfo : ArchiveFileInfo
    {
        public VapFileEntry Entry { get; }

        public VapArchiveFileInfo(Stream fileData, string filePath, VapFileEntry entry) :
            base(fileData, filePath)
        {
            Entry = entry;
        }

        public VapArchiveFileInfo(Stream fileData, string filePath, IKompressionConfiguration configuration, long decompressedSize) :
            base(fileData, filePath, configuration, decompressedSize)
        {
        }
    }

    class VapSupport
    {
        public static string DetermineExtension(Stream input)
        {
            var magicSamples = CollectMagicSamples(input);

            if (magicSamples.Any(x => x.Contains("CGFX")))
                return ".cgfx";

            return ".bin";
        }

        private static IList<string> CollectMagicSamples(Stream input)
        {
            var bkPos = input.Position;

            using var br = new BinaryReaderX(input, true);

            // Get 3 samples to check magic with compression
            input.Position = bkPos;
            var magic1 = br.ReadString(4);
            input.Position = bkPos + 1;
            var magic2 = br.ReadString(4);
            input.Position = bkPos + 2;
            var magic3 = br.ReadString(4);

            return new[] { magic1, magic2, magic3 };
        }
    }
}
