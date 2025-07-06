using Komponent.IO;

namespace plugin_koei_tecmo.Archives
{
    class IdxEntry
    {
        public int size;
        public int offset;
    }

    class IdxSupport
    {
        public static string DetermineExtension(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            switch (br.ReadString(4))
            {
                case "GT1G":
                    return ".g1t";

                case "_A1G":
                    return ".g1a";

                case "SMDH":
                    return ".icn";

                default:
                    return ".bin";
            }
        }
    }
}
