namespace Konnect.Contract.Plugin.File.Archive;

/// <summary>
/// Marks the archive state able to remove files.
/// </summary>
public interface IRemoveFiles : IArchiveFilePluginState
{
    /// <summary>
    /// Removes a single file from the archive state.
    /// </summary>
    /// <param name="file">The file to remove.</param>
    void RemoveFile(IArchiveFile file);

    /// <summary>
    /// Removes all files from the archive state.
    /// </summary>
    void RemoveAll();
}