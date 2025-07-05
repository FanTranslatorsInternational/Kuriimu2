using Komponent.IO;

namespace plugin_kadokawa.Archives
{
    class EncFileEntry
    {
        public uint size;
        public int decompSize;
        public int offset;

        public bool IsCompressed => (size & 0x80000000) > 0;
    }

    class EncSupport
    {
        public static string DetermineExtension(Stream input)
        {
            var magicSamples = CollectMagicSamples(input);

            if (magicSamples.Any(x => x.Contains("CTX")))
                return ".ctx";

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

            return [magic1, magic2, magic3];
        }
    }
}
