using plugin_level5.Common.Archive.Models;

namespace plugin_level5.Common.Archive
{
    internal class ArchiveComposer
    {
        private readonly ArchiveWriterFactory _writerFactory = new();

        public void Compose(ArchiveData data, Stream output)
        {
            IArchiveWriter writer = _writerFactory.Create(data.ArchiveType);
            writer.Write(data, output);
        }
    }
}
