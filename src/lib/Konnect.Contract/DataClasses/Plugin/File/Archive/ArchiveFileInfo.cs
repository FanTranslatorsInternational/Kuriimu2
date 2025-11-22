using Konnect.Contract.DataClasses.FileSystem;

namespace Konnect.Contract.DataClasses.Plugin.File.Archive;

public class ArchiveFileInfo
{
    public required UPath FilePath { get; set; }
    public required Stream FileData { get; set; }

    public Guid[]? PluginIds { get; set; }

    public bool ContentChanged { get; set; }
}