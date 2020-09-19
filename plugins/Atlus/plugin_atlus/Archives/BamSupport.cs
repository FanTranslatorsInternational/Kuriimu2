using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Attributes;

namespace plugin_atlus.Archives
{
    class BamHeader
    {
        [FixedLength(4)]
        public string magic;

        public int size;
        public int zero0;
        public int zero1;
        public int zero2;
        public int dataStart;
    }

    class BamSubHeader
    {
        [FixedLength(8)]
        public string magic;

        public int size;
    }

    class BamSupport
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
