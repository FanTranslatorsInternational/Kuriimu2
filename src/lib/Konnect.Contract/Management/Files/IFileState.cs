using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Streams;
using Konnect.Contract.Plugin.File;

namespace Konnect.Contract.Management.Files;

/// <summary>
/// Exposes properties for a loaded file in the Kuriimu runtime.
/// </summary>
public interface IFileState : IDisposable
{
    /// <summary>
    /// The plugin manager for this state.
    /// </summary>
    IPluginFileManager FileManager { get; }

    /// <summary>
    /// The entry class of the plugin for this file.
    /// </summary>
    IFilePlugin FilePlugin { get; }

    /// <summary>
    /// The state of the plugin for this file.
    /// </summary>
    IFilePluginState PluginState { get; }

    /// <summary>
    /// The path of the file initially opened for this state relative to the file system.
    /// <see cref="UPath.Empty"/> if a file of this format was newly created.
    /// </summary>
    UPath FilePath { get; }

    /// <summary>
    /// The absolute directory of the file system for this state over all parents.
    /// </summary>
    UPath AbsoluteDirectory { get; }

    /// <summary>
    /// The file system <see cref="FilePath"/> is relative to.
    /// The file system is rooted to <see cref="FilePath"/>.
    /// </summary>
    IFileSystem FileSystem { get; }

    /// <summary>
    /// The stream manager for this state.
    /// </summary>
    IStreamManager StreamManager { get; }

    /// <summary>
    /// All child states that were opened from this one.
    /// </summary>
    IList<IFileState> ArchiveChildren { get; }

    /// <summary>
    /// The parent state from which this file was opened.
    /// <see langword="null" /> if this file wasn't opened from another state.
    /// </summary>
    IFileState? ParentFileState { get; }

    /// <summary>
    /// The values retrieved by dialogs in the initial load process.
    /// </summary>
    IList<string> DialogOptions { get; }

    /// <summary>
    /// Gets a value determining if the state has a parent.
    /// </summary>
    bool HasParent { get; }

    /// <summary>
    /// Gets a value determining if the plugin state changed.
    /// </summary>
    bool StateChanged { get; }

    /// <summary>
    /// Determines if this state is disposed.
    /// </summary>
    bool IsDisposed { get; }

    /// <summary>
    /// Sets <see cref="FileSystem"/> and <see cref="FilePath"/> for a new input file.
    /// </summary>
    /// <param name="fileSystem">The new file system for this state.</param>
    /// <param name="filePath">The new file path relative to the file system for this state.</param>
    void SetNewFileInput(IFileSystem fileSystem, UPath filePath);

    /// <summary>
    /// Sets dialog options for this state.
    /// </summary>
    /// <param name="options"></param>
    void SetDialogOptions(IList<string> options);

    /// <summary>
    /// Renames the <see cref="FilePath"/> of the opened file.
    /// </summary>
    /// <param name="renamedPath">The renamed path.</param>
    void RenameFilePath(UPath renamedPath);
}