using System.IO;
using Komponent.IO;
using Kontract.Models.Archive;

namespace plugin_mercury_steam.Archives
{
    class PkgHeader
    {
        public int tableSize;
        public int dataSize;
        public int fileCount;
    }

    class PkgEntry
    {
        public uint hash;
        public int startOffset;
        public int endOffset;
    }

    class PkgArchiveFileInfo : ArchiveFileInfo
    {
        public string Type { get; private set; }
        public uint Hash { get; }

        public PkgArchiveFileInfo(Stream fileData, string filePath, uint hash) : base(fileData, filePath)
        {
            Hash = hash;
            Type = PkgSupport.DetermineMagic(fileData);
        }

        public override void SetFileData(Stream fileData)
        {
            base.SetFileData(fileData);

            Type = PkgSupport.DetermineMagic(fileData);
        }
    }

    class PkgSupport
    {
        public static string DetermineMagic(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            var position = input.Position;
            input.Position = 0;
            var magic = br.ReadString(4);
            input.Position = position;

            return magic;
        }

        public static int DetermineAlignment(string magic)
        {
            if (magic == "MMDL")
                return 4;

            return 0x80;
        }
    }
}
