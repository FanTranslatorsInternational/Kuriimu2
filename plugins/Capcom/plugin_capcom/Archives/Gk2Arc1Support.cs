using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Kontract.Kompression.Configuration;
using Kontract.Models.Archive;

namespace plugin_capcom.Archives
{
    class Gk2Arc1Entry
    {
        public int offset;
        public uint size;

        public int FileSize
        {
            get => (int)(size & 0x7FFFFFFF);
            set => size = (uint)((size & ~0x7FFFFFFF) | value);
        }

        public bool IsCompressed => (size & 0x80000000) != 0;
    }

    class Gk1Arc1ArchiveFileInfo : ArchiveFileInfo
    {
        public Gk2Arc1Entry Entry { get; }

        public Gk1Arc1ArchiveFileInfo(Stream fileData, string filePath, Gk2Arc1Entry entry) : base(fileData, filePath)
        {
            Entry = entry;
        }

        public Gk1Arc1ArchiveFileInfo(Stream fileData, string filePath, Gk2Arc1Entry entry, IKompressionConfiguration configuration, long decompressedSize) : base(fileData, filePath, configuration, decompressedSize)
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

            return new[] { magic1, magic2, magic3 };
        }
    }
}
