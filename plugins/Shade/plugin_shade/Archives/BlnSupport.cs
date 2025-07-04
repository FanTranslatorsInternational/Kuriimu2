using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_shade.Archives
{
    class Mcb0Entry
    {
        public short id;
        public short unk2;
        public uint offset;
        public uint size;
    }

    class BlnArchiveFile : ArchiveFile
    {
        public Mcb0Entry Entry { get; }

        public BlnArchiveFile(ArchiveFileInfo fileInfo, Mcb0Entry entry) : base(fileInfo)
        {
            Entry = entry;
        }
    }
}
