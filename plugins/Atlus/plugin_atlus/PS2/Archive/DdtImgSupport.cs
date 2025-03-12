using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_atlus.PS2.Archive
{
    public class DdtEntry
    {
        public uint nameOffset;
        public uint entryOffset;
        public int entrySize;
    }    

    public class DdtArchiveFile : ArchiveFile
    {
        public DdtEntry Entry { get; }
        public bool IsFile => Entry.entrySize >= 0;

        public DdtArchiveFile(ArchiveFileInfo fileInfo, DdtEntry entry) : base(fileInfo)
        {
            Entry = entry;
        }
    }
}
