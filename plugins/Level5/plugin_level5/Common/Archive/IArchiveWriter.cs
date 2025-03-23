using plugin_level5.Common.Archive.Models;

namespace plugin_level5.Common.Archive
{
    public interface IArchiveWriter
    {
        void Write(ArchiveData data, Stream output);
    }
}
