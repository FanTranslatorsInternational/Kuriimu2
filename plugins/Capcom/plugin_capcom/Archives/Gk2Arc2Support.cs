using Komponent.IO;

namespace plugin_capcom.Archives
{
    class Gk2Arc2Support
    {
        public static string DetermineExtension(Stream input)
        {
            var magicSamples = CollectMagicSamples(input);

            if (magicSamples.Any(x => x.Contains("RECN")))
                return ".ncer";

            if (magicSamples.Any(x => x.Contains("RNAN")))
                return ".nanr";

            if (magicSamples.Any(x => x.Contains("RGCN")))
                return ".ncgr";

            if (magicSamples.Any(x => x.Contains("RLCN")))
                return ".nclr";

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
