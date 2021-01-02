using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Kontract.Interfaces.Progress;
using Kontract.Kompression.Configuration;
using Kontract.Models.Archive;

namespace plugin_inti_creates.Archives
{
    class FntFileEntry
    {
        public int offset;
        public int endOffset;
    }

    class FntArchiveFileInfo : ArchiveFileInfo
    {
        public FntArchiveFileInfo(Stream fileData, string filePath) :
            base(fileData, filePath)
        {
        }

        public FntArchiveFileInfo(Stream fileData, string filePath, IKompressionConfiguration configuration, long decompressedSize) :
            base(fileData, filePath, configuration, decompressedSize)
        {
        }

        public override long SaveFileData(Stream output, bool compress, IProgressContext progress = null)
        {
            var writtenSize = base.SaveFileData(output, compress, progress);

            while (output.Position % 0x80 > 0)
                output.WriteByte(0);

            return writtenSize;
        }
    }

    class FntSupport
    {
        public static string DetermineExtension(Stream input)
        {
            var magicSamples = CollectMagicSamples(input);

            if (magicSamples.Any(x => x.Contains("CTPK")))
                return ".ctpk";

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
