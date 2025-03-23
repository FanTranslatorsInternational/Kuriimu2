using plugin_level5.Common.Archive.Models;

namespace plugin_level5.Common.Archive
{
    public class ArchiveWriterFactory
    {
        public IArchiveWriter Create(ArchiveType type)
        {
            switch (type)
            {
                case ArchiveType.Xpck:
                    return new XpckWriter();

                case ArchiveType.Xfsp:
                    return new XfspWriter();

                default:
                    throw new InvalidOperationException($"Unknown archive type {type}.");
            }
        }
    }
}
