using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_level5.Mobile.Archive
{
    struct Hp10Header
    {
        public string magic; // HP10
        public int fileCount;
        public uint fileSize;

        public int stringEnd;
        public int stringOffset;
        public int dataOffset;

        public short unk1; // 0x800
        public short unk2; // 0x800
        public int zero1;
    }

    public class Hp10FileEntry
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

    public class Hp10ArchiveFile : ArchiveFile
    {
        public Hp10FileEntry Entry { get; }

        public Hp10ArchiveFile(ArchiveFileInfo fileInfo, Hp10FileEntry entry) : base(fileInfo)
        {
            Entry = entry;
        }
    }
}
