using Konnect.Contract.Plugin.File.Archive;
using Konnect.DataClasses.FileSystem;
using Konnect.Extensions;

namespace plugin_atlus.PS2.Archive
{
    public class DdtEntry
    {
        public uint nameOffset;
        public uint entryOffset;
        public int entrySize;
    }

    class DdtInfoHolder
    {
        public DdtEntry Entry { get; } = new();
        public DirectoryEntry? Directory { get; }
        public IArchiveFile? File { get; }

        public bool IsFile => File != null;

        public string Name => File?.FilePath.GetName() ?? Directory!.Name;

        public DdtInfoHolder(DirectoryEntry entry)
        {
            Directory = entry;
        }

        public DdtInfoHolder(IArchiveFile fileInfo)
        {
            File = fileInfo;
        }
    }
}
