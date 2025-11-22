using Konnect.Contract.Plugin.File.Archive;

namespace Konnect.Contract.DataClasses.FileSystem;

public class AfiFileEntry : FileEntry
{
    public required IArchiveFile ArchiveFile { get; init; }
}