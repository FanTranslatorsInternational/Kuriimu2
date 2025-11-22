using Konnect.Contract.DataClasses.FileSystem;

namespace Konnect.Contract.Plugin.File.Archive;

/// <summary>
/// Exposes methods to rename files in an archive.
/// </summary>
public interface IRenameFiles : IArchiveFilePluginState
{
    /// <summary>
    /// Rename a given file.
    /// </summary>
    /// <param name="file">The file to rename.</param>
    /// <param name="path">The new path of the file.</param>
    void RenameFile(IArchiveFile file, UPath path);
}