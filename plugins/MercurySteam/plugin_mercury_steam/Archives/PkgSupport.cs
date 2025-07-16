using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

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

    class PkgArchiveFile : ArchiveFile
    {
        public string Type { get; private set; }
        public uint Hash { get; }

        public PkgArchiveFile(ArchiveFileInfo fileInfo, uint hash) : base(fileInfo)
        {
            Type = PkgSupport.DetermineMagic(fileInfo.FileData);
            Hash = hash;
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
            switch (magic)
            {
                case "MTXT":
                    return 0x80;

                default:
                    return 0x4;
            }
        }
    }
}
