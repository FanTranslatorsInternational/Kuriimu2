using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_capcom.Archives
{
    // HINT: Hashes are CRC32/JAMCRC
    class ObbHeader
    {
        public string magic;
        public int version;
        public int fileCount;
        public uint crc;
    }

    class ObbEntry
    {
        public uint pathHash;
        public int offset;
        public int size;
        public uint unkHash;
    }

    class ObbArchiveFile : ArchiveFile
    {
        public ObbEntry Entry { get; }

        public ObbArchiveFile(ArchiveFileInfo fileInfo, ObbEntry entry) : base(fileInfo)
        {
            Entry = entry;
        }
    }

    class ObbSupport
    {
        public static string DetermineExtension(Stream input)
        {
            var magicSamples = CollectMagicSamples(input);

            if (magicSamples.Any(x => x.Contains("OggS")))
                return ".ogg";

            if (magicSamples.Any(x => x.Contains("ARC\0")))
                return ".arc";

            if (magicSamples.Any(x => x.Contains("GUI\0")))
                return ".gui";

            if (magicSamples.Any(x => x.Contains("PRPZ")))
                return ".prp";

            if (magicSamples.Any(x => x.Contains("FWSE")))
                return ".swf";

            if (magicSamples.Any(x => x.Contains("SRQR")))
                return ".srq";

            if (magicSamples.Any(x => x.Contains("STQR")))
                return ".stqr";

            if (magicSamples.Any(x => x.Contains("UIKC")))
                return ".uik";

            if (magicSamples.Any(x => x.Contains("TEX ")))
                return ".tex";

            if (magicSamples.Any(x => x.Contains("SBKR")))
                return ".sbk";

            if (magicSamples.Any(x => x.Contains("GMD\0")))
                return ".gmd";

            if (magicSamples.Any(x => x.Contains("SDL\0")))
                return ".sdl";

            if (magicSamples.Any(x => x.Contains("EFL\0")))
                return ".efl";

            if (magicSamples.Any(x => x.Contains("EAN\0")))
                return ".ean";

            if (magicSamples.Any(x => x.Contains("MOD\0")))
                return ".mod";

            if (magicSamples.Any(x => x.Contains("MRL\0")))
                return ".mrl";

            if (magicSamples.Any(x => x.Contains("XFS\0")))
                return ".xfs";

            if (magicSamples.Any(x => x.Contains("LMT\0")))
                return ".lmt";

            if (magicSamples.Any(x => x.Contains("E2D\0")))
                return ".e2d";

            if (magicSamples.Any(x => x.Contains("LCM\0")))
                return ".lcm";

            if (magicSamples.Any(x => x.Contains("SBC")))
                return ".sbc";

            if (magicSamples.Any(x => x.Contains("GFD\0")))
                return ".gfd";

            if (magicSamples.Any(x => x.Contains("MFX\0")))
                return ".mfx";

            return ".bin";
        }

        private static IList<string> CollectMagicSamples(Stream input)
        {
            var bkPos = input.Position;

            using var br = new BinaryReaderX(input, true);

            // Get 3 samples to check magic with compression
            input.Position = bkPos;
            var magic1 = br.ReadString(4);
            input.Position = bkPos + 4;
            var magic2 = br.ReadString(4);
            input.Position = bkPos + 8;
            var magic3 = br.ReadString(4);

            return [magic1, magic2, magic3];
        }
    }
}
