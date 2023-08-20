using System.IO;
using Komponent.IO.Attributes;
using Kontract.Kompression.Interfaces.Configuration;
using Kontract.Models.Plugins.State.Archive;

namespace plugin_level5.Mobile.Archives
{
    class Hp10Header
    {
        [FixedLength(4)]
        public string magic = "HP10";
        public int fileCount;
        public uint fileSize;

        public int stringEnd;
        public int stringOffset;
        public int dataOffset;

        public short unk1 = 0x800;
        public short unk2 = 0x800;
        public int zero1;
    }

    class Hp10FileEntry
    {
        public uint crc32bFileNameHash;
        public uint crc32cFileNameHash;
        public uint crc32bFilePathHash;
        public uint crc32cFilePathHash;

        public uint fileOffset;
        public int fileSize;
        public int nameOffset;
        public uint timestamp;
    }

    class Hp10ArchiveFileInfo : ArchiveFileInfo
    {
        public Hp10FileEntry Entry { get; }

        public Hp10ArchiveFileInfo(Stream fileData, string filePath, Hp10FileEntry entry) : base(fileData, filePath)
        {
            Entry = entry;
        }

        public Hp10ArchiveFileInfo(Stream fileData, string filePath, IKompressionConfiguration configuration, long decompressedSize) : base(fileData, filePath, configuration, decompressedSize)
        {
        }
    }
}
