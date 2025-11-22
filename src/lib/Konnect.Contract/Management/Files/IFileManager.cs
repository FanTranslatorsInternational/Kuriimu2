using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Management.Files;
using Konnect.Contract.DataClasses.Management.Files.Events;
using Konnect.Contract.FileSystem;
using Serilog;

namespace Konnect.Contract.Management.Files;

public delegate Task ManualSelectionDelegate(ManualSelectionEventArgs e);

public interface IFileManager : IPluginFileManager
{
    /// <summary>
    /// An event to allow for manual selection by the user.
    /// </summary>
    event ManualSelectionDelegate? OnManualSelection;

    /// <summary>
    /// Declares if manual plugin selection on Load is allowed.
    /// </summary>
    bool AllowManualSelection { get; set; }

    /// <summary>
    /// The logger for this plugin manager.
    /// </summary>
    ILogger Logger { get; set; }

    /// <summary>
    /// Gets a loaded file, or <see langword="null"/> if not loaded.
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    IFileState? GetLoadedFile(UPath filePath);

    /// <summary>
    /// Identifies a file path against a given plugin.
    /// </summary>
    /// <param name="file">The physical file to identify.</param>
    /// <param name="pluginId">The plugin ID to identify with.</param>
    /// <returns>If the file could be identified by the denoted plugin.</returns>
    Task<bool> CanIdentify(string file, Guid pluginId);

    /// <summary>
    /// Loads a physical path into the Kuriimu runtime.
    /// </summary>
    /// <param name="file">The path to the path to load.</param>
    /// <returns>The loaded state of the path.</returns>
    Task<LoadResult> LoadFile(string file);

    /// <summary>
    /// Loads a physical path into the Kuriimu runtime.
    /// </summary>
    /// <param name="file">The path to the path to load.</param>
    /// <param name="pluginId">the plugin with which to load the file.</param>
    /// <returns>The loaded state of the path.</returns>
    Task<LoadResult> LoadFile(string file, Guid pluginId);

    /// <summary>
    /// Loads a physical path into the Kuriimu runtime.
    /// </summary>
    /// <param name="file">The path to the path to load.</param>
    /// <param name="loadFileContext">The context with additional parameters for the load process.</param>
    /// <returns>The loaded state of the path.</returns>
    Task<LoadResult> LoadFile(string file, LoadFileContext loadFileContext);

    /// <summary>
    /// Loads a file from a given file system.
    /// </summary>
    /// <param name="fileSystem">The file system to load the file from.</param>
    /// <param name="path">The file to load from the file system.</param>
    /// <param name="parentFileState">The state from which the file system originates.</param>
    /// <returns>The loaded <see cref="IFileState"/> for the file.</returns>
    Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, IFileState parentFileState);

    /// <summary>
    /// Loads a file from a given file system.
    /// </summary>
    /// <param name="fileSystem">The file system to load the file from.</param>
    /// <param name="path">The file to load from the file system.</param>
    /// <param name="pluginId">The Id of the plugin to load the file with.</param>
    /// <param name="parentFileState">The state from which the file system originates.</param>
    /// <returns>The loaded <see cref="IFileState"/> for the file.</returns>
    Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, Guid pluginId, IFileState parentFileState);

    /// <summary>
    /// Loads a file from a given file system.
    /// </summary>
    /// <param name="fileSystem">The file system to load the file from.</param>
    /// <param name="path">The file to load from the file system.</param>
    /// <param name="parentFileState">The state from which the file system originates.</param>
    /// <param name="loadFileContext">The context with additional parameters for the load process.</param>
    /// <returns>The loaded <see cref="IFileState"/> for the file.</returns>
    Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, IFileState parentFileState, LoadFileContext loadFileContext);

    /// <summary>
    /// Save a loaded state to a physical path.
    /// </summary>
    /// <param name="fileState">The <see cref="IFileState"/> to save.</param>
    /// <param name="saveFile">The physical path at which to save the file.</param>
    /// <returns></returns>
    Task<SaveResult> SaveFile(IFileState fileState, string saveFile);
}