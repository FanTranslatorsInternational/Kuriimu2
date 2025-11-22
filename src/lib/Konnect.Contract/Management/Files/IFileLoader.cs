using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Management.Files;
using Konnect.Contract.FileSystem;

namespace Konnect.Contract.Management.Files;

/// <summary>
/// Exposes methods to load files into a plugin state.
/// </summary>
public interface IFileLoader
{
    /// <summary>
    /// An event to allow for manual selection by the user.
    /// </summary>
    event ManualSelectionDelegate? OnManualSelection;

    /// <summary>
    /// Loads any file from a given file system.
    /// </summary>
    /// <param name="fileSystem">The file system to load the file from.</param>
    /// <param name="filePath">The path into the file system.</param>
    /// <param name="loadFileOptions">The load context for this load action.</param>
    /// <returns>The loaded state of the file.</returns>
    Task<LoadResult> LoadAsync(IFileSystem fileSystem, UPath filePath, LoadFileOptions loadFileOptions);
}