using plugin_level5.Common.Archive.Models;

namespace plugin_level5.Common.Archive
{
    public interface IArchiveReader
    {
        ArchiveData Read(Stream input);
    }
}
