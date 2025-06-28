using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Progress;
using Konnect.Plugin.File.Archive;

namespace plugin_bandai_namco.Archives
{
    class AmbHeader
    {
        public string magic = "#AMB";
        public int headerLength = 0x20;
        public int zero0;
        public int unk1;
        public int fileCount;
        public int fileEntryStart;
        public int dataOffset;
        public int zero1;
    }

    class AmbFileEntry
    {
        public int offset;
        public int size;
        public int unk1;
        public int zero0;
    }

    class AmbArchiveFile : ArchiveFile
    {
        public AmbFileEntry Entry { get; }

        public AmbArchiveFile(ArchiveFileInfo fileInfo, AmbFileEntry entry) : base(fileInfo)
        {
            Entry = entry;
        }
    }

    class AmbSupport
    {
        public static string DetermineExtension(Stream input)
        {
            var magicSamples = CollectMagicSamples(input);

            if (magicSamples.Any(x => x.Contains("CTPK")))
                return ".ctpk";

            if (magicSamples.Any(x => x.Contains("#AME")))
                return ".ame";

            if (magicSamples.Any(x => x.Contains("#AMO")))
                return ".amo";

            if (magicSamples.Any(x => x.Contains("BCH")))
                return ".bch";

            if (magicSamples.Any(x => x.Contains("#AMK")))
                return ".amk";

            if (magicSamples.Any(x => x.Contains("#BSK")))
                return ".bsk";

            if (magicSamples.Any(x => x.Contains("TXT")))
                return ".txt";

            if (magicSamples.Any(x => x.Contains("#AMB")))
                return ".amb";

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

            return [magic1, magic2, magic3];
        }
    }
}
