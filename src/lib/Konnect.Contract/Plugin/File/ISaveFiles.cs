using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;

namespace Konnect.Contract.Plugin.File;

/// <summary>
/// Marks the plugin state as saveable and exposes methods to save the current state.
/// </summary>
public interface ISaveFiles : IFilePluginState
{
    /// <summary>
    /// Determine if the state got modified.
    /// </summary>
    bool ContentChanged { get; }

    /// <summary>
    /// Try to save the current state to a file.
    /// </summary>
    /// <param name="fileSystem">The file system to save the state into.</param>
    /// <param name="savePath">The new path to the initial file.</param>
    /// <param name="saveContext">The context for this save operation, containing environment instances.</param>
    /// <returns>If the save procedure was successful.</returns>
    Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext);
}