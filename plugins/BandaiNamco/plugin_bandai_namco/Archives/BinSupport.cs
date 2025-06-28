using Komponent.IO;

namespace plugin_bandai_namco.Archives
{
    class BinSupport
    {
        public static string DetermineExtension(Stream input)
        {
            var magicSamples = CollectMagicSamples(input);

            if (magicSamples.Any(x => x.Contains("darc")))
                return ".arc";

            if (magicSamples.Any(x => x.Contains("CTPK")))
                return ".ctpk";

            if (magicSamples.Any(x => x.Contains("CFNT")))
                return ".cfnt";

            return ".bin";
        }

        private static IList<string> CollectMagicSamples(Stream input)
        {
            var bkPos = input.Position;

            using var br = new BinaryReaderX(input, true);

            // Get 2 samples to check magic with compression
            input.Position = bkPos;
            var magic1 = br.ReadString(4);
            input.Position = bkPos + 0x10;
            var magic2 = br.ReadString(4);

            return new[] { magic1, magic2 };
        }
    }
}
