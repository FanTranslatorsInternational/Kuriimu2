using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Management.Files;
using Konnect.Contract.DataClasses.Management.Files.Events;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Management.Files;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Dialog;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Management.Plugin;
using Konnect.Contract.Management.Streams;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Contract.Progress;
using Konnect.Extensions;
using Konnect.FileSystem;
using Konnect.Management.Dialog;
using Konnect.Management.Streams;
using Konnect.Progress;
using Serilog;
using System.Diagnostics.CodeAnalysis;

namespace Konnect.Management.Files;

/// <summary>
/// The core component of the Kuriimu runtime.
/// </summary>
public class FileManager : IFileManager
{
    private readonly IPluginManager _pluginManager;
    private readonly FileLoader _fileLoader;
    private readonly FileSaver _fileSaver;
    private readonly StreamMonitor _streamMonitor;

    private ILogger? _logger;

    private readonly List<UPath> _loadingFiles = [];
    private readonly object _loadingLock = new();

    private readonly List<IFileState> _loadedFiles = [];
    private readonly object _loadedFilesLock = new();

    private readonly List<IFileState> _savingStates = [];
    private readonly object _saveLock = new();

    private readonly List<IFileState> _closingStates = [];
    private readonly object _closeLock = new();

    /// <inheritdoc />
    public event ManualSelectionDelegate? OnManualSelection;

    /// <inheritdoc />
    public bool AllowManualSelection { get; set; }

    public IProgressContext Progress { get; set; } = new ProgressContext(new NullProgressOutput());

    public IFilePreferences? Preferences { get; init; }

    public IDialogManager? DialogManager { get; init; }

    public ILogger? Logger
    {
        get => _logger;
        set => SetLogger(value);
    }

    #region Constructors

    /// <summary>
    /// Creates a new instance of <see cref="FileManager"/>.
    /// </summary>
    /// <param name="pluginManager">The plugin manager for this instance.</param>
    public FileManager(IPluginManager pluginManager)
    {
        _streamMonitor = new StreamMonitor();

        _pluginManager = pluginManager;
        _fileLoader = new FileLoader(pluginManager);
        _fileSaver = new FileSaver(_streamMonitor);

        _fileLoader.OnManualSelection += FileLoader_OnManualSelection;
    }

    #endregion

    #region Get Methods

    /// <inheritdoc />
    public IFileState? GetLoadedFile(UPath filePath)
    {
        lock (_loadedFilesLock)
        {
            return _loadedFiles.FirstOrDefault(x => UPath.Combine(x.AbsoluteDirectory, x.FilePath.ToRelative()) == filePath);
        }
    }

    #endregion

    #region Check

    /// <inheritdoc />
    public bool IsLoading(UPath filePath)
    {
        lock (_loadingLock)
        {
            return _loadingFiles.Any(x => x == filePath);
        }
    }

    /// <inheritdoc />
    public bool IsLoaded(UPath filePath)
    {
        lock (_loadedFilesLock)
        {
            return _loadedFiles.Any(x => UPath.Combine(x.AbsoluteDirectory, x.FilePath.ToRelative()) == filePath);
        }
    }

    /// <inheritdoc />
    public bool IsSaving(IFileState fileState)
    {
        lock (_saveLock)
        {
            return _savingStates.Contains(fileState);
        }
    }

    /// <inheritdoc />
    public bool IsClosing(IFileState fileState)
    {
        lock (_closeLock)
        {
            return _closingStates.Contains(fileState);
        }
    }

    #endregion

    #region Identfy File

    public async Task<Guid?> Identify(string file)
    {
        var identifiablePlugins = _pluginManager.GetPlugins<IFilePlugin>().Where(p => p.CanIdentifyFiles).Cast<IIdentifyFiles>();

        var matchedPlugins = new List<IFilePlugin>();
        foreach (IIdentifyFiles identifiablePlugin in identifiablePlugins)
        {
            var canIdentify = await Identify(file, identifiablePlugin);
            if (!canIdentify)
                continue;

            if (matchedPlugins.Count >= 1)
                return null;

            matchedPlugins.Add(identifiablePlugin);
        }

        return matchedPlugins.Count > 0 ? matchedPlugins[0].PluginId : null;
    }

    public async Task<bool> Identify(string file, Guid pluginId)
    {
        // 1. Check if identification is even supported
        if (!SupportsIdentify(pluginId, out IIdentifyFiles? identifiablePlugin))
            return false;

        return await Identify(file, identifiablePlugin);
    }

    public async Task<bool> Identify(string file, IIdentifyFiles plugin)
    {
        // 2. Create file system
        var streamManager = CreateStreamManager();
        var fileSystem = FileSystemFactory.CreatePhysicalFileSystem(streamManager);
        var filePath = fileSystem.ConvertPathFromInternal(file);

        var root = filePath.GetRoot();
        fileSystem = FileSystemFactory.CreateSubFileSystem(fileSystem, root);

        // 3. Identify file
        return await Identify(fileSystem, filePath.GetSubDirectory(root), streamManager, plugin);
    }

    public async Task<bool> Identify(IFileState fileState, IArchiveFile afi, Guid pluginId)
    {
        // 1. Check if identification is even supported
        if (!SupportsIdentify(pluginId, out IIdentifyFiles? identifiablePlugin))
            return false;

        // 2. Create file system
        var streamManager = CreateStreamManager();
        var fileSystem = FileSystemFactory.CreateArchivePluginFileSystem(fileState, UPath.Root, streamManager);

        // 3. Identify file
        return await Identify(fileSystem, afi.FilePath, streamManager, identifiablePlugin);
    }

    public async Task<bool> Identify(StreamFile streamFile, Guid pluginId)
    {
        // 1. Check if identification is even supported
        if (!SupportsIdentify(pluginId, out IIdentifyFiles? identifiablePlugin))
            return false;

        // 2. Create file system
        var streamManager = CreateStreamManager();
        var fileSystem = FileSystemFactory.CreateMemoryFileSystem(streamFile, streamManager);

        // 3. Identify file
        return await Identify(fileSystem, streamFile.Path.ToAbsolute(), streamManager, identifiablePlugin);
    }

    public async Task<bool> Identify(IFileSystem fileSystem, UPath path, Guid pluginId)
    {
        // 1. Check if identification is even supported
        if (!SupportsIdentify(pluginId, out IIdentifyFiles? identifiablePlugin))
            return false;

        // 2. Create controlled file system
        var streamManager = CreateStreamManager();
        var clonedFileSystem = fileSystem.Clone(streamManager);

        // 3. Identify file
        return await Identify(clonedFileSystem, path, streamManager, identifiablePlugin);
    }

    private bool SupportsIdentify(Guid pluginId, [NotNullWhen(true)] out IIdentifyFiles? identifiablePlugin)
    {
        var plugin = _pluginManager.GetPlugin<IFilePlugin>(pluginId);
        bool supportsIdentify = plugin is { CanIdentifyFiles: true };

        identifiablePlugin = supportsIdentify ? (IIdentifyFiles?)plugin : null;

        return supportsIdentify;
    }

    private static async Task<bool> Identify(IFileSystem fileSystem, UPath path, IStreamManager streamManager, IIdentifyFiles plugin)
    {
        try
        {
            var identifyContext = new IdentifyContext
            {
                TemporaryStreamManager = streamManager.CreateTemporaryStreamProvider()
            };
            return await plugin.IdentifyAsync(fileSystem, path, identifyContext);
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            streamManager.ReleaseAll();
        }
    }

    #endregion

    #region Load File

    #region Load Physical

    /// <inheritdoc />
    public Task<LoadResult> LoadFile(string file)
    {
        return LoadFile(file, new LoadFileContext
        {
            Logger = Logger
        });
    }

    /// <inheritdoc />
    public Task<LoadResult> LoadFile(string file, Guid pluginId)
    {
        return LoadFile(file, new LoadFileContext
        {
            Logger = Logger,
            PluginId = pluginId
        });
    }

    /// <inheritdoc />
    public async Task<LoadResult> LoadFile(string file, LoadFileContext loadFileContext)
    {
        // 1. Create file system
        var streamManager = CreateStreamManager();
        var fileSystem = FileSystemFactory.CreatePhysicalFileSystem(streamManager);
        var filePath = fileSystem.ConvertPathFromInternal(file);

        var root = filePath.GetRoot();
        fileSystem = FileSystemFactory.CreateSubFileSystem(fileSystem, root);

        // If file is already loaded or loading
        lock (_loadingLock)
        {
            if (_loadingFiles.Any(x => x == file))
                return new LoadResult
                {
                    Status = LoadStatus.Errored,
                    Reason = LoadErrorReason.Loading
                };

            if (IsLoaded(file))
                return new LoadResult
                {
                    Status = LoadStatus.Successful,
                    LoadedFileState = GetLoadedFile(file),
                    Reason = LoadErrorReason.None
                };

            _loadingFiles.Add(file);
        }

        // 3. Load file
        // Physical files don't have a parent, if loaded like this
        var loadedFile = await LoadFile(fileSystem, filePath.GetSubDirectory(root), streamManager, null, loadFileContext);

        lock (_loadingLock)
            _loadingFiles.Remove(file);

        return loadedFile;
    }

    #endregion

    #region Load ArchiveFileInfo

    /// <inheritdoc />
    public Task<LoadResult> LoadFile(IFileState fileState, IArchiveFile afi)
    {
        return LoadFile(fileState, afi, new LoadFileContext
        {
            Logger = Logger
        });
    }

    /// <inheritdoc />
    public Task<LoadResult> LoadFile(IFileState fileState, IArchiveFile afi, Guid pluginId)
    {
        return LoadFile(fileState, afi, new LoadFileContext
        {
            Logger = Logger,
            PluginId = pluginId
        });
    }

    /// <inheritdoc />
    public async Task<LoadResult> LoadFile(IFileState fileState, IArchiveFile afi, LoadFileContext loadFileContext)
    {
        // If fileState is no archive state
        if (!fileState.PluginState.IsArchive)
            throw new InvalidOperationException("The state represents no archive.");

        // If file is already loaded or loading
        var absoluteFilePath = UPath.Combine(fileState.AbsoluteDirectory, fileState.FilePath.ToRelative(), afi.FilePath.ToRelative());
        lock (_loadingLock)
        {
            if (_loadingFiles.Any(x => x == absoluteFilePath))
                return new LoadResult
                {
                    Status = LoadStatus.Errored,
                    Reason = LoadErrorReason.Loading
                };

            if (IsLoaded(absoluteFilePath))
                return new LoadResult
                {
                    Status = LoadStatus.Successful,
                    LoadedFileState = GetLoadedFile(absoluteFilePath),
                    Reason = LoadErrorReason.None
                };

            _loadingFiles.Add(absoluteFilePath);
        }

        // 1. Create file system
        var streamManager = CreateStreamManager();
        var fileSystem = FileSystemFactory.CreateArchivePluginFileSystem(fileState, UPath.Root, streamManager);

        // 2. Load file
        // IArchiveFileInfos have fileState as their parent, if loaded like this
        var loadResult = await LoadFile(fileSystem, afi.FilePath, streamManager, fileState, loadFileContext);
        if (loadResult.Status != LoadStatus.Successful || loadResult.LoadedFileState is null)
        {
            lock (_loadingLock)
                _loadingFiles.Remove(absoluteFilePath);

            return loadResult;
        }

        // 3. Add archive child to parent
        // ArchiveChildren are only added, if a file is loaded like this
        fileState.ArchiveChildren.Add(loadResult.LoadedFileState);

        lock (_loadingLock)
            _loadingFiles.Remove(absoluteFilePath);

        return loadResult;
    }

    #endregion

    #region Load FileSystem

    /// <inheritdoc />
    public Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path)
    {
        return LoadFile(fileSystem, path, null, new LoadFileContext
        {
            Logger = Logger
        });
    }

    /// <inheritdoc />
    public Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, Guid pluginId)
    {
        return LoadFile(fileSystem, path, null, new LoadFileContext
        {
            Logger = Logger,
            PluginId = pluginId
        });
    }

    /// <inheritdoc />
    public Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, IFileState parentFileState)
    {
        return LoadFile(fileSystem, path, parentFileState, new LoadFileContext
        {
            Logger = Logger
        });
    }

    /// <inheritdoc />
    public Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, Guid pluginId, IFileState parentFileState)
    {
        return LoadFile(fileSystem, path, parentFileState, new LoadFileContext
        {
            Logger = Logger,
            PluginId = pluginId
        });
    }

    /// <inheritdoc />
    public Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, LoadFileContext loadFileContext)
    {
        return LoadFile(fileSystem, path, null, loadFileContext);
    }

    /// <inheritdoc />
    public async Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, IFileState? parentFileState, LoadFileContext loadFileContext)
    {
        // Downside of not having ArchiveChildren is not having the states saved below automatically when opened file is saved

        // If file is loaded
        var absoluteFilePath = UPath.Combine(fileSystem.ConvertPathToInternal(UPath.Root), path.ToRelative());
        lock (_loadingLock)
        {
            if (_loadingFiles.Any(x => x == absoluteFilePath))
                return new LoadResult
                {
                    Status = LoadStatus.Errored,
                    Reason = LoadErrorReason.Loading
                };

            if (IsLoaded(absoluteFilePath))
                return new LoadResult
                {
                    Status = LoadStatus.Successful,
                    LoadedFileState = GetLoadedFile(absoluteFilePath),
                    Reason = LoadErrorReason.None
                };

            _loadingFiles.Add(absoluteFilePath);
        }

        // 1. Create file system action
        var streamManager = CreateStreamManager();
        fileSystem = fileSystem.Clone(streamManager);

        // 2. Load file
        // Only if called by a ScopedFileManager the parent state is not null
        // Does not add ArchiveChildren to parent state
        var loadedFile = await LoadFile(fileSystem, path.ToAbsolute(), streamManager, parentFileState, loadFileContext);

        lock (_loadingLock)
            _loadingFiles.Remove(absoluteFilePath);

        return loadedFile;
    }

    #endregion

    #region Load Stream

    /// <inheritdoc />
    public Task<LoadResult> LoadFile(StreamFile streamFile)
    {
        return LoadFile(streamFile, new LoadFileContext
        {
            Logger = Logger
        });
    }

    /// <inheritdoc />
    public Task<LoadResult> LoadFile(StreamFile streamFile, Guid pluginId)
    {
        return LoadFile(streamFile, new LoadFileContext
        {
            Logger = Logger,
            PluginId = pluginId
        });
    }

    /// <inheritdoc />
    public Task<LoadResult> LoadFile(StreamFile streamFile, LoadFileContext loadFileContext)
    {
        // We don't check for an already loaded file here, since that should never happen

        // 1. Create file system action
        var streamManager = CreateStreamManager();
        var fileSystem = FileSystemFactory.CreateMemoryFileSystem(streamFile, streamManager);

        // 2. Load file
        // A stream has no parent, since it should never occur to be loaded from somewhere deeper in the system
        return LoadFile(fileSystem, streamFile.Path.ToAbsolute(), streamManager, null, loadFileContext);
    }

    #endregion

    private async Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, IStreamManager streamManager, IFileState? parentFileState, LoadFileContext loadFileContext)
    {
        // 1. Select plugin and options
        (IFilePlugin? plugin, IList<string> options) = SelectFromCache(fileSystem, path, loadFileContext);

        var isRunning = Progress.IsRunning();
        if (!isRunning) Progress.StartProgress();

        // 2. Load file
        IDialogManager dialogManager = DialogManager != null
            ? new PredefinedDialogManager(DialogManager, options)
            : new PredefinedDialogManager(options);
        var loadResult = await _fileLoader.LoadAsync(fileSystem, path, new LoadFileOptions
        {
            ParentFileState = parentFileState,
            StreamManager = streamManager,
            FileManager = this,
            Plugin = plugin,
            Progress = Progress,
            DialogManager = dialogManager,
            AllowManualSelection = AllowManualSelection,
            Logger = loadFileContext.Logger
        });

        if (!isRunning) Progress.FinishProgress();

        // 3. Persist plugin and options
        SetToCache(loadResult);

        // 4. Add file to loaded files
        lock (_loadedFilesLock)
            if (loadResult is { Status: LoadStatus.Successful, LoadedFileState: not null })
                _loadedFiles.Add(loadResult.LoadedFileState);

        return loadResult;
    }

    private (IFilePlugin?, IList<string>) SelectFromCache(IFileSystem fileSystem, UPath path, LoadFileContext loadFileContext)
    {
        Guid pluginId = loadFileContext.PluginId;
        List<string> options = loadFileContext.Options;

        if (Preferences is not null)
        {
            UPath absolutePath = fileSystem.ConvertPathToInternal(path);
            FilePreferenceEntry? cacheEntry = Preferences.GetOrDefault(absolutePath.FullName ?? string.Empty);

            if (cacheEntry is not null)
            {
                if (pluginId == Guid.Empty)
                {
                    pluginId = cacheEntry.Value.PluginId;

                    options.Clear();
                    options.AddRange(cacheEntry.Value.Options);
                }
                else
                {
                    if (pluginId == cacheEntry.Value.PluginId && options.Count != cacheEntry.Value.Options.Count)
                    {
                        options.Clear();
                        options.AddRange(cacheEntry.Value.Options);
                    }
                }
            }
        }

        IFilePlugin? plugin = ResolvePluginIdOrDefault(pluginId);

        return (plugin, loadFileContext.Options);
    }

    private IFilePlugin? ResolvePluginIdOrDefault(Guid pluginId)
    {
        IFilePlugin? plugin = null;
        if (pluginId != Guid.Empty)
            plugin = _pluginManager.GetPlugin<IFilePlugin>(pluginId);

        return plugin;
    }

    private void SetToCache(LoadResult result)
    {
        if (Preferences is null)
            return;

        if (result.LoadedFileState is null)
            return;

        if (!result.LoadedFileState.WasPluginManuallySelected && result.LoadedFileState.DialogFields.Count <= 0)
            return;

        var options = new List<string>();
        options.AddRange(result.LoadedFileState.DialogFields.Select(x => x.Result!));

        var element = new FilePreferenceEntry(result.LoadedFileState.FilePlugin.PluginId, options);

        UPath absolutePath = result.LoadedFileState.FileSystem.ConvertPathToInternal(result.LoadedFileState.FilePath);
        if (absolutePath.IsNull)
            return;

        Preferences.Set(absolutePath.FullName!, element);
    }

    #endregion

    #region Save File

    // TODO: Add archive children as saving files as well to reduce race conditions

    /// <inheritdoc />
    public Task<SaveResult> SaveFile(IFileState fileState)
    {
        return SaveFile(fileState, fileState.FileSystem, fileState.FilePath.FullName);
    }

    /// <inheritdoc />
    public Task<SaveResult> SaveFile(IFileState fileState, string saveFile)
    {
        var fileSystem = FileSystemFactory.CreatePhysicalFileSystem(fileState.StreamManager);
        var savePath = fileSystem.ConvertPathFromInternal(saveFile);

        var root = savePath.GetRoot();
        fileSystem = FileSystemFactory.CreateSubFileSystem(fileSystem, root);

        return SaveFile(fileState, fileSystem, savePath.GetSubDirectory(root));
    }

    // TODO: Put in options from external call like in Load
    /// <inheritdoc />
    public async Task<SaveResult> SaveFile(IFileState fileState, IFileSystem fileSystem, UPath savePath)
    {
        if (fileState.IsDisposed)
            return new SaveResult
            {
                IsSuccessful = false,
                Reason = SaveErrorReason.Closed
            };

        lock (_saveLock)
        {
            if (_savingStates.Contains(fileState))
                return new SaveResult
                {
                    IsSuccessful = false,
                    Reason = SaveErrorReason.Saving
                };

            if (IsClosing(fileState))
                return new SaveResult
                {
                    IsSuccessful = false,
                    Reason = SaveErrorReason.Closing
                };

            _savingStates.Add(fileState);
        }

        lock (_loadedFilesLock)
            if (!_loadedFiles.Contains(fileState))
                return new SaveResult
                {
                    IsSuccessful = false,
                    Reason = SaveErrorReason.NotLoaded
                };

        var isRunning = Progress.IsRunning();
        if (!isRunning) Progress.StartProgress();

        var saveResult = await _fileSaver.SaveAsync(fileState, fileSystem, savePath, new SaveFileOptions
        {
            Progress = Progress,
            DialogManager = DialogManager,
            Logger = Logger
        });

        if (!isRunning) Progress.FinishProgress();

        lock (_saveLock)
            _savingStates.Remove(fileState);

        return saveResult;
    }

    #endregion

    #region Save Stream

    public async Task<SaveStreamResult> SaveStream(IFileState fileState)
    {
        if (fileState.IsDisposed)
            return new SaveStreamResult
            {
                IsSuccessful = false,
                Reason = SaveErrorReason.Closed
            };

        lock (_saveLock)
        {
            if (_savingStates.Contains(fileState))
                return new SaveStreamResult
                {
                    IsSuccessful = false,
                    Reason = SaveErrorReason.Saving
                };

            if (IsClosing(fileState))
                return new SaveStreamResult
                {
                    IsSuccessful = false,
                    Reason = SaveErrorReason.Closing
                };

            _savingStates.Add(fileState);
        }

        lock (_loadedFilesLock)
            if (!_loadedFiles.Contains(fileState))
                return new SaveStreamResult
                {
                    IsSuccessful = false,
                    Reason = SaveErrorReason.NotLoaded
                };

        var isRunning = Progress.IsRunning();
        if (!isRunning) Progress.StartProgress();

        // Save to memory file system
        var fileSystem = new MemoryFileSystem(fileState.StreamManager);
        var saveResult = await _fileSaver.SaveAsync(fileState, fileSystem, fileState.FilePath, new SaveFileOptions
        {
            Progress = Progress,
            DialogManager = DialogManager,
            Logger = Logger
        });

        if (!isRunning) Progress.FinishProgress();

        lock (_saveLock)
            _savingStates.Remove(fileState);

        if (!saveResult.IsSuccessful)
            return new SaveStreamResult
            {
                IsSuccessful = false,
                Exception = saveResult.Exception,
                Reason = saveResult.Reason
            };

        // Collect all StreamFiles from memory file system
        var streamFiles = fileSystem.EnumerateAllFiles(UPath.Root).Select(x =>
            new StreamFile
            {
                Stream = fileSystem.OpenFile(x, FileMode.Open, FileAccess.Read, FileShare.Read),
                Path = x
            }).ToArray();

        return new SaveStreamResult
        {
            IsSuccessful = true,
            SavedStreams = streamFiles,
            Reason = SaveErrorReason.None
        };
    }

    #endregion

    #region Close File

    /// <inheritdoc />
    public CloseResult Close(IFileState fileState)
    {
        if (fileState.IsDisposed)
            return new CloseResult
            {
                IsSuccessful = true,
                Reason = CloseErrorReason.None
            };

        lock (_closeLock)
        {
            if (_closingStates.Contains(fileState))
                return new CloseResult
                {
                    IsSuccessful = false,
                    Reason = CloseErrorReason.Closing
                };

            if (IsSaving(fileState))
                return new CloseResult
                {
                    IsSuccessful = false,
                    Reason = CloseErrorReason.Saving
                };

            _closingStates.Add(fileState);
        }

        lock (_loadedFilesLock)
            if (!_loadedFiles.Contains(fileState))
                return new CloseResult
                {
                    IsSuccessful = false,
                    Reason = CloseErrorReason.NotLoaded
                };

        // Remove state from its parent
        fileState.ParentFileState?.ArchiveChildren.Remove(fileState);

        CloseInternal(fileState);

        lock (_closeLock)
            _closingStates.Remove(fileState);

        return new CloseResult
        {
            IsSuccessful = true,
            Reason = CloseErrorReason.None
        };
    }

    /// <inheritdoc />
    public void CloseAll()
    {
        lock (_loadedFilesLock)
        {
            foreach (var stateInfo in _loadedFiles)
            {
                lock (_closeLock)
                {
                    if (_closingStates.Contains(stateInfo))
                        return;

                    if (IsSaving(stateInfo))
                        return;

                    _closingStates.Add(stateInfo);
                }

                stateInfo.Dispose();

                lock (_closeLock)
                    _closingStates.Remove(stateInfo);
            }

            _loadedFiles.Clear();
        }
    }

    private void CloseInternal(IFileState fileState)
    {
        // Close children of this state first
        foreach (var child in fileState.ArchiveChildren)
            CloseInternal(child);

        // Close indirect children of this state
        // Indirect children occur when a file is loaded by a FileSystem and got a parent attached manually
        IList<IFileState> indirectChildren;
        lock (_loadedFilesLock)
            indirectChildren = [.. _loadedFiles.Where(x => x.ParentFileState == fileState)];

        foreach (var indirectChild in indirectChildren)
            CloseInternal(indirectChild);

        lock (_loadedFilesLock)
        {
            // Close state itself
            if (_streamMonitor.Manages(fileState.StreamManager))
                _streamMonitor.RemoveStreamManager(fileState.StreamManager);
            fileState.Dispose();

            // Remove from the file tracking of this instance
            _loadedFiles.Remove(fileState);
        }
    }

    #endregion

    public void Dispose()
    {
        CloseAll();

        _streamMonitor.Dispose();
    }

    private async Task FileLoader_OnManualSelection(ManualSelectionEventArgs e)
    {
        if (OnManualSelection == null)
            return;

        await OnManualSelection.Invoke(e);
    }

    private void SetLogger(ILogger? logger)
    {
        _logger = logger;
        _streamMonitor.Logger = logger;
    }

    private IStreamManager CreateStreamManager()
    {
        return _streamMonitor.CreateStreamManager();
    }
}