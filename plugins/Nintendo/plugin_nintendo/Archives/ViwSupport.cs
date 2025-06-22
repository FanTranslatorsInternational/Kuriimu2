using Komponent.IO;

namespace plugin_nintendo.Archives
{
    struct ViwEntry
    {
        public int id;
        public string name;
    }

    struct ViwInfHeader
    {
        public int fileCount;
        public int metaCount;
        public int entryOffset;
        public int metaOffset;
    }

    struct ViwInfEntry
    {
        public int offset;
        public int compSize;
    }

    struct ViwInfMetaEntry
    {
        public short unk1;
        public short unk2;
        public int unk3;
    }

    class ViwSupport
    {
        public static string DetermineExtension(Stream input)
        {
            var magicSamples = CollectMagicSamples(input);

            if (magicSamples.Any(x => x.Contains("DFV")))
                return ".vfd";

            if (magicSamples.Any(x => x.Contains("BCA0")))
                return ".bca";

            if (magicSamples.Any(x => x.Contains("BMD0")))
                return ".bmd";

            if (magicSamples.Any(x => x.Contains("BMA0")))
                return ".bma";

            if (magicSamples.Any(x => x.Contains("BTA0")))
                return ".bta";

            if (magicSamples.Any(x => x.Contains("BTP0")))
                return ".btp";

            if (magicSamples.Any(x => x.Contains("BVA0")))
                return ".bva";

            if (magicSamples.Any(x => x.Contains("OWLV")))
                return ".owlv";

            if (magicSamples.Any(x => x.Contains("GCV")))
                return ".vcg";

            if (magicSamples.Any(x => x.Contains("LCV")))
                return ".vcl";

            if (magicSamples.Any(x => x.Contains("CSV")))
                return ".vsc";

            if (magicSamples.Any(x => x.Contains("ECV")))
                return ".vce";

            if (magicSamples.Any(x => x.Contains("OPV")))
                return ".vpo";

            return ".bin";
        }

        private static IList<string> CollectMagicSamples(Stream input)
        {
            var bkPos = input.Position;

            using var br = new BinaryReaderX(input, true);

            // Get 3 samples to check magic with compression
            input.Position = bkPos + 4;
            var magic1 = br.ReadString(4);
            input.Position = bkPos + 5;
            var magic2 = br.ReadString(4);
            input.Position = bkPos + 6;
            var magic3 = br.ReadString(4);

            return [magic1, magic2, magic3];
        }
    }
}
