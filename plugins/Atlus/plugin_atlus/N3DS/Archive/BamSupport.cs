using Komponent.Contract.Aspects;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_atlus.N3DS.Archive
{
    struct BamHeader
    {
        public string magic;
        public int size;
        public int zero0;
        public int zero1;
        public int zero2;
        public int dataStart;
    }

    struct BamSubHeader
    {
        public string magic;
        public int size;
    }

    public class BamFileInfo
    {
        public int offset;
        public int size;
        public uint hash;
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

            using var br = new BinaryReaderX(input);

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

    public class BamArchiveFile : ArchiveFile
    {
        public BamArchiveFile(ArchiveFileInfo fileInfo) : base(fileInfo)
        {
        }
    }
}
