namespace Konnect.Contract.Plugin.File.Archive;

/// <summary>
/// Exposes methods to replace file data.
/// </summary>
public interface IReplaceFiles : IArchiveFilePluginState
{
    /// <summary>
    /// Replaces file data in a given file.
    /// </summary>
    /// <param name="file">The file to replace data in.</param>
    /// <param name="fileData">The new file data to replace the original file with.</param>
    void ReplaceFile(IArchiveFile file, Stream fileData);
}