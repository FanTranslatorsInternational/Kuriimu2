using Kompression.Contract;

namespace Konnect.Contract.DataClasses.Plugin.File.Archive;

public class CompressedArchiveFileInfo : ArchiveFileInfo
{
    public required ICompression Compression { get; set; }
    public required int DecompressedSize { get; set; }
}