using plugin_level5.Common.Compression;

namespace plugin_level5.Common.Archive.Models
{
    public class ArchiveData
    {
        public ArchiveType ArchiveType { get; set; }
        public byte ContentType { get; set; }
        public Level5CompressionMethod StringCompression { get; set; }
        public IList<ArchiveNamedEntry> Files { get; set; }
    }
}
