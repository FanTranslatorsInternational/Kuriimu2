using System.IO;
using Komponent.IO;
using Komponent.IO.Attributes;
using Kontract.Models.Archive;

namespace plugin_primula.Archives
{
    class Pac2Header
    {
        [FixedLength(12)]
        public string magic = "GAMEDAT PAC2";
        public int fileCount;
    }

    class Pac2Entry
    {
        public int Position { get; set; }
        public int Size { get; set; }
    }

    class Pac2ArchiveFileInfo : ArchiveFileInfo
    {
        public string Type { get; private set; }

        public Pac2ArchiveFileInfo(Stream fileData, string filePath) : base(fileData, filePath)
        {
            Type = Pac2Support.DetermineMagic(fileData);
        }

        public override void SetFileData(Stream fileData)
        {
            base.SetFileData(fileData);

            Type = Pac2Support.DetermineMagic(fileData);
        }
    }

    class Pac2Support
    {
        public static string DetermineMagic(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            var position = input.Position;
            input.Position = 0;
            var magic = br.ReadString(12);
            input.Position = position;

            return magic;
        }
    }
}
