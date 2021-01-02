using Komponent.Extensions;
using Kontract.Extensions;
using Kontract.Models.Archive;

namespace plugin_atlus.Archives
{
    class DdtEntry
    {
        public uint nameOffset;
        public uint entryOffset;
        public int entrySize;

        public bool IsFile => entrySize >= 0;
    }

    class DdtInfoHolder
    {
        public DdtEntry Entry { get; }
        public DirectoryEntry Directory { get; }
        public IArchiveFileInfo File { get; }

        public bool IsFile => File != null;

        public string Name => File?.FilePath.GetName() ?? Directory.Name;

        public DdtInfoHolder(DirectoryEntry entry)
        {
            Directory = entry;
            Entry = new DdtEntry();
        }

        public DdtInfoHolder(IArchiveFileInfo fileInfo)
        {
            File = fileInfo;
            Entry = new DdtEntry();
        }
    }
}
