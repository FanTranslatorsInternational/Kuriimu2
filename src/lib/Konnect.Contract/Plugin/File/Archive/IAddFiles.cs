using Konnect.Contract.DataClasses.FileSystem;

namespace Konnect.Contract.Plugin.File.Archive;

/// <summary>
/// Marks the archive state able to add a new file.
/// </summary>
public interface IAddFiles : IArchiveFilePluginState
{
    /// <summary>
    /// Adds a file to the archive state.
    /// </summary>
    /// <param name="fileData">The file stream to set to the <see cref="IArchiveFile"/>.</param>
    /// <param name="filePath">The path of the file to add to this state.</param>
    /// <returns>The newly created <see cref="IArchiveFile"/>.</returns>
    IArchiveFile AddFile(Stream fileData, UPath filePath);
}