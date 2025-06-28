using Komponent.IO;

namespace plugin_bandai_namco.Archives
{
    class _3dsLzSupport
    {
        public static string DetermineExtension(Stream input)
        {
            input.Position += 4;
            var magicSamples = CollectMagicSamples(input);
            input.Position -= 4;

            if (magicSamples.Any(x => x.Contains("Lua")))
                return ".lua";

            if (magicSamples.Any(x => x.Contains("BAE")))
                return ".bae";

            if (magicSamples.Any(x => x.Contains("TOTX")))
                return ".ttx";

            if (magicSamples.Any(x => x.Contains("BCH")))
                return ".bch";

            if (magicSamples.Any(x => x.Contains("BSK")))
                return ".bsk";

            if (magicSamples.Any(x => x.Contains("DEM")))
                return ".dem";

            if (magicSamples.Any(x => x.Contains("AMC")))
                return ".amc";

            if (magicSamples.Any(x => x.Contains("AMK")))
                return ".amk";

            if (magicSamples.Any(x => x.Contains("BCSV")))
                return ".csv";

            if (magicSamples.Any(x => x.Contains("CFNT")))
                return ".bcfnt";

            if (magicSamples.Any(x => x.Contains("DVLB")))
                return ".dvb";

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
