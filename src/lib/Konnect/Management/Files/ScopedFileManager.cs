using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Management.Files;
using Konnect.Contract.Enums.Management.Files;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;

namespace Konnect.Management.Files;

/// <summary>
/// A nested <see cref="IPluginFileManager"/> for passing into plugins and controlling their behaviour.
/// </summary>
class ScopedFileManager(IFileManager parentFileManager) : IPluginFileManager
{
    private IFileState? _fileState;

    private readonly List<IFileState> _loadedFiles = [];

    public void RegisterStateInfo(IFileState fileState)
    {
        _fileState = fileState;
    }

    #region Check

    /// <inheritdoc />
    public bool IsLoading(UPath filePath)
    {
        return parentFileManager.IsLoading(filePath);
    }

    /// <inheritdoc />
    public bool IsLoaded(UPath filePath)
    {
        return parentFileManager.IsLoaded(filePath);
    }

    /// <inheritdoc />
    public bool IsSaving(IFileState fileState)
    {
        return parentFileManager.IsSaving(fileState);
    }

    /// <inheritdoc />
    public bool IsClosing(IFileState fileState)
    {
        return parentFileManager.IsClosing(fileState);
    }

    #endregion

    #region Identify file

    public Task<bool> Identify(IFileState fileState, IArchiveFile afi, Guid pluginId)
    {
        return parentFileManager.Identify(fileState, afi, pluginId);
    }

    public Task<bool> Identify(StreamFile streamFile, Guid pluginId)
    {
        return parentFileManager.Identify(streamFile, pluginId);
    }

    public Task<bool> Identify(IFileSystem fileSystem, UPath path, Guid pluginId)
    {
        return parentFileManager.Identify(fileSystem, path, pluginId);
    }

    #endregion

    #region Load File

    #region Load FileSystem

    /// <inheritdoc />
    public Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path)
    {
        return LoadFile(fileSystem, path, new LoadFileContext
        {
            Logger = parentFileManager.Logger
        });
    }

    /// <inheritdoc />
    public Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, Guid pluginId)
    {
        return LoadFile(fileSystem, path, new LoadFileContext
        {
            Logger = parentFileManager.Logger,
            PluginId = pluginId
        });
    }

    /// <inheritdoc />
    public async Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, LoadFileContext loadFileContext)
    {
        ArgumentNullException.ThrowIfNull(_fileState);

        // If the same file is passed to another plugin, take the parent of the current state
        var parent = _fileState;
        var statePath = _fileState.AbsoluteDirectory / _fileState.FilePath.ToRelative();
        if (fileSystem.ConvertPathToInternal(path) == statePath)
            parent = _fileState.ParentFileState;

        // 1. Load file
        var loadResult = await parentFileManager.LoadFile(fileSystem, path, parent, loadFileContext);
        if (loadResult.Status != LoadStatus.Successful)
            return loadResult;

        // 2. Add file to loaded files
        _loadedFiles.Add(loadResult.LoadedFileState!);

        return loadResult;
    }

    #endregion

    #region Load ArchiveFileInfo

    /// <inheritdoc />
    public Task<LoadResult> LoadFile(IFileState fileState, IArchiveFile afi)
    {
        return parentFileManager.LoadFile(fileState, afi);
    }

    /// <inheritdoc />
    public Task<LoadResult> LoadFile(IFileState fileState, IArchiveFile afi, Guid pluginId)
    {
        return parentFileManager.LoadFile(fileState, afi, new LoadFileContext
        {
            Logger = parentFileManager.Logger,
            PluginId = pluginId
        });
    }

    /// <inheritdoc />
    public Task<LoadResult> LoadFile(IFileState fileState, IArchiveFile afi, LoadFileContext loadFileContext)
    {
        return parentFileManager.LoadFile(fileState, afi, loadFileContext);
    }

    #endregion

    #region Load Stream

    /// <inheritdoc />
    public Task<LoadResult> LoadFile(StreamFile streamFile)
    {
        return LoadFile(streamFile, new LoadFileContext
        {
            Logger = parentFileManager.Logger
        });
    }

    /// <inheritdoc />
    public Task<LoadResult> LoadFile(StreamFile streamFile, Guid pluginId)
    {
        return LoadFile(streamFile, new LoadFileContext
        {
            Logger = parentFileManager.Logger,
            PluginId = pluginId
        });
    }

    /// <inheritdoc />
    public async Task<LoadResult> LoadFile(StreamFile streamFile, LoadFileContext loadFileContext)
    {
        // 1. Load file
        var loadResult = await parentFileManager.LoadFile(streamFile, loadFileContext);
        if (loadResult.Status != LoadStatus.Successful)
            return loadResult;

        // 2. Add file to loaded files
        _loadedFiles.Add(loadResult.LoadedFileState!);

        return loadResult;
    }

    #endregion

    #endregion

    #region Save File

    public Task<SaveResult> SaveFile(IFileState fileState)
    {
        return parentFileManager.SaveFile(fileState);
    }

    public Task<SaveResult> SaveFile(IFileState fileState, IFileSystem fileSystem, UPath savePath)
    {
        return parentFileManager.SaveFile(fileState, fileSystem, savePath);
    }

    #endregion

    #region Save Stream

    public Task<SaveStreamResult> SaveStream(IFileState fileState)
    {
        return parentFileManager.SaveStream(fileState);
    }

    #endregion

    #region Close file

    public CloseResult Close(IFileState fileState)
    {
        var closeResult = parentFileManager.Close(fileState);
        _loadedFiles.Remove(fileState);

        return closeResult;
    }

    public void CloseAll()
    {
        foreach (var loadedFile in _loadedFiles)
            parentFileManager.Close(loadedFile);

        _loadedFiles.Clear();
    }

    #endregion
}