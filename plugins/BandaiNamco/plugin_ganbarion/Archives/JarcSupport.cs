using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_ganbarion.Archives
{
    class JcmpHeader
    {
        public string magic="jCMP";
        public int fileSize;
        public int unk1;
        public int compSize;
        public int decompSize;
    }

    class JarcHeader
    {
        public string magic = "jARC";
        public int fileSize;
        public int unk1;
        public int fileCount;
    }

    class JarcEntry
    {
        public int fileOffset;
        public int fileSize;
        public int nameOffset;
        public uint hash;
        public int unk1;
    }

    class JarcArchiveFile : ArchiveFile
    {
        public JarcEntry Entry { get; }

        public JarcArchiveFile(ArchiveFileInfo fileInfo, JarcEntry entry) : base(fileInfo)
        {
            Entry = entry;
        }
    }
}
