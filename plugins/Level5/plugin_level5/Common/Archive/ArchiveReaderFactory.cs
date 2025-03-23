using plugin_level5.Common.Archive.Models;

namespace plugin_level5.Common.Archive
{
    public class ArchiveReaderFactory
    {
        public IArchiveReader Create(ArchiveType type)
        {
            switch (type)
            {
                case ArchiveType.Xpck:
                    return new XpckReader();

                case ArchiveType.Xfsp:
                    return new XfspReader();

                default:
                    throw new InvalidOperationException($"Unknown archive type {type}.");
            }
        }
    }
}
