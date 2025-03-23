using plugin_level5.Common.Archive.Models;

namespace plugin_level5.Common.Archive
{
    internal class ArchiveParser
    {
        private readonly ArchiveTypeReader _typeReader = new();
        private readonly ArchiveReaderFactory _readerFactory = new();

        public ArchiveData Parse(Stream input)
        {
            ArchiveType type = _typeReader.Peek(input);
            IArchiveReader reader = _readerFactory.Create(type);

            return reader.Read(input);
        }
    }
}
