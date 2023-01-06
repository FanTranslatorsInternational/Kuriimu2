using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Kontract.Kompression.Interfaces.Configuration;
using Kontract.Models.Plugins.State.Archive;

namespace plugin_level5._3DS.Archives
{
    class FLArchiveFileInfo : ArchiveFileInfo
    {
        public int FileId { get; }

        public FLArchiveFileInfo(Stream fileData, string filePath, int fileId) : base(fileData, filePath)
        {
            FileId = fileId;
        }

        public FLArchiveFileInfo(Stream fileData, string filePath, int fileId, IKompressionConfiguration configuration, long decompressedSize) : base(fileData, filePath, configuration, decompressedSize)
        {
            FileId = fileId;
        }
    }

    class FLSupport
    {
        public static string DetermineExtension(Stream input)
        {
            var magicSamples = CollectMagicSamples(input);

            if (magicSamples.Any(x => x.Contains("ztex")))
                return ".ztex";

            if (magicSamples.Any(x => x.Contains("zmdl")))
                return ".zmdl";

            return ".bin";
        }

        private static IList<string> CollectMagicSamples(Stream input)
        {
            var bkPos = input.Position;

            using var br = new BinaryReaderX(input, true);

            // Get 3 samples to check magic with compression
            input.Position = bkPos + 5;
            var magic1 = br.ReadString(4);
            input.Position = bkPos + 6;
            var magic2 = br.ReadString(4);
            input.Position = bkPos + 7;
            var magic3 = br.ReadString(4);

            input.Position = bkPos;
            return new[] { magic1, magic2, magic3 };
        }
    }
}
