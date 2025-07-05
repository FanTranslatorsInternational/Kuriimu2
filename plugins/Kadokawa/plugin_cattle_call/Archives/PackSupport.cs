using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_cattle_call.Archives
{
    class PackEntry
    {
        public uint hash;
        public int offset;
        public int size;
    }

    class PackArchiveFile : ArchiveFile
    {
        public PackEntry Entry { get; }

        public PackArchiveFile(ArchiveFileInfo fileInfo, PackEntry entry) : base(fileInfo)
        {
            Entry = entry;
        }
    }
}
