using Konnect.Contract.DataClasses.FileSystem;

namespace Konnect.Contract.Plugin.File.Archive;

/// <summary>
/// Marks the state to be an archive and exposes properties to retrieve and modify file data from the state.
/// </summary>
public interface IArchiveFilePluginState : IFilePluginState
{
    /// <summary>
    /// The read-only collection of files the current archive contains.
    /// </summary>
    IReadOnlyList<IArchiveFile> Files { get; }

    #region Optional feature support checks

    bool CanReplaceFiles => this is IReplaceFiles;
    bool CanRenameFiles => this is IRenameFiles;
    bool CanDeleteFiles => this is IRemoveFiles;
    bool CanAddFiles => this is IAddFiles;

    #endregion

    #region Optional feature casting defaults

    void AttemptReplaceFile(IArchiveFile afi, Stream fileData) => (this as IReplaceFiles)?.ReplaceFile(afi, fileData);
    void AttemptRenameFile(IArchiveFile afi, UPath path) => (this as IRenameFiles)?.RenameFile(afi, path);
    void AttemptRemoveFile(IArchiveFile afi) => (this as IRemoveFiles)?.RemoveFile(afi);
    void AttemptRemoveAll() => (this as IRemoveFiles)?.RemoveAll();
    IArchiveFile? AttemptAddFile(Stream fileData, UPath filePath) => (this as IAddFiles)?.AddFile(fileData, filePath);

    #endregion
}