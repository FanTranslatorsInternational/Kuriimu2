using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_inti_creates.Archives
{
    class VapHeader
    {
        public int fileCount;
        public int unk1;
        public int zero0;
    }

    class VapFileEntry
    {
        public int offset;
        public int size;
        public int unk1;
        public int unk2;
    }

    class VapArchiveFile : ArchiveFile
    {
        public VapFileEntry Entry { get; }

        public VapArchiveFile(ArchiveFileInfo fileInfo, VapFileEntry entry) : base(fileInfo)
        {
            Entry = entry;
        }
    }

    class VapSupport
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

            return [magic1, magic2, magic3];
        }
    }
}
