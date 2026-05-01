using plugin_level5.Common.Compression;

namespace plugin_level5.Common.Archive.Models
{
    public class ArchiveData
    {
        public required ArchiveType ArchiveType { get; set; }
        public required byte ContentType { get; set; }
        public Level5CompressionMethod StringCompression { get; set; }
        public required List<ArchiveNamedEntry> Files { get; set; }
    }
}
