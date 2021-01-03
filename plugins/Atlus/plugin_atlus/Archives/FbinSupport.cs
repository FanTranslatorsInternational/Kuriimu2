using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Attributes;
#pragma warning disable 649

namespace plugin_atlus.Archives
{
    class FbinHeader
    {
        [FixedLength(4)]
        public string magic = "FBIN";

        public int fileCount;
        public int dataOffset;
    }

    class FbinSupport
    {
        public static string DetermineExtension(Stream input)
        {
            var magicSamples = CollectMagicSamples(input);

            if (magicSamples.Any(x => x.Contains("GTSF")))
                return ".gtsf";

            if (magicSamples.Any(x => x.Contains("GFAN")))
                return ".gfan";

            if (magicSamples.Any(x => x.Contains("T2B1")))
                return ".t2b";

            if (magicSamples.Any(x => x.Contains("FLW0")))
                return ".flw";

            if (magicSamples.Any(x => x.Contains("TBB1")))
                return ".tbb";

            return ".bin";
        }

        private static IList<string> CollectMagicSamples(Stream input)
        {
            var bkPos = input.Position;

            using var br = new BinaryReaderX(input, true);

            // Get 3 samples to check magic with compression
            input.Position = bkPos;
            var magic1 = br.ReadString(4);
            input.Position = bkPos + 4;
            var magic2 = br.ReadString(4);
            input.Position = bkPos + 8;
            var magic3 = br.ReadString(4);

            return new[] { magic1, magic2, magic3 };
        }
    }
}
