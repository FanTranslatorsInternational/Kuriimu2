using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_capcom.Archives
{
    class Gk2Arc1Entry
    {
        public int offset;
        public uint size;

        public uint FileSize
        {
            get => size & 0x7FFFFFFF;
            set => size = (size & ~0x7FFFFFFFu) | value;
        }

        public bool IsCompressed => (size & 0x80000000) != 0;
    }

    class Gk2Arc1ArchiveFile : ArchiveFile
    {
        public Gk2Arc1Entry Entry { get; }

        public Gk2Arc1ArchiveFile(ArchiveFileInfo fileInfo, Gk2Arc1Entry entry) : base(fileInfo)
        {
            Entry = entry;
        }
    }

    class Gk2Arc1Support
    {
        public static string DetermineExtension(Stream input, bool isCompressed)
        {
            var magicSamples = CollectMagicSamples(input, isCompressed);

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

        private static IList<string> CollectMagicSamples(Stream input, bool isCompressed)
        {
            var bkPos = input.Position;

            using var br = new BinaryReaderX(input, true);

            // Get 3 samples to check magic with compression
            input.Position = bkPos + (isCompressed ? 4 : 0);
            var magic1 = br.ReadString(4);
            input.Position = bkPos + (isCompressed ? 4 : 0) + 1;
            var magic2 = br.ReadString(4);
            input.Position = bkPos + (isCompressed ? 4 : 0) + 2;
            var magic3 = br.ReadString(4);

            return [magic1, magic2, magic3];
        }
    }
}
