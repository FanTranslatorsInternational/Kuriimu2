using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;

namespace Konnect.Contract.Plugin.File;

/// <summary>
/// Marks the plugin as loadable and exposes methods to load a file into the state.
/// </summary>
public interface ILoadFiles : IFilePluginState
{
    /// <summary>
    /// Load the file into the state.
    /// </summary>
    /// <param name="fileSystem">The file system from which the file is requested.</param>
    /// <param name="filePath">The path to the file requested by the user.</param>
    /// <param name="loadContext">The context for this load operation, containing environment instances.</param>
    /// <returns>If the load procedure was successful.</returns>
    Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext);
}