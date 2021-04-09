using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kontract;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Loaders;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Game;
using Kontract.Interfaces.Progress;
using Kontract.Models;
using Kontract.Models.Archive;
using Kontract.Models.Context;
using Kontract.Models.IO;
using Kore.Factories;
using Kore.FileSystem.Implementations;
using Kore.Managers.Plugins.FileManagement;
using Kore.Managers.Plugins.PluginLoader;
using Kore.Models;
using Kore.Models.LoadInfo;
using Kore.Progress;
using MoreLinq;
using Serilog;

namespace Kore.Managers.Plugins
{
    /// <summary>
    /// The core component of the Kuriimu runtime.
    /// </summary>
    public class PluginManager : IInternalPluginManager
    {
        private readonly IPluginLoader<IFilePlugin>[] _filePluginLoaders;
        private readonly IPluginLoader<IGameAdapter>[] _gameAdapterLoaders;

        private readonly IFileLoader _fileLoader;
        private readonly IFileSaver _fileSaver;

        private readonly StreamMonitor _streamMonitor;

        private readonly IList<UPath> _loadingFiles = new List<UPath>();
        private readonly object _loadingLock = new object();

        private readonly IList<IStateInfo> _loadedFiles = new List<IStateInfo>();
        private readonly object _loadedFilesLock = new object();

        private readonly IList<IStateInfo> _savingStates = new List<IStateInfo>();
        private readonly object _saveLock = new object();

        private readonly IList<IStateInfo> _closingStates = new List<IStateInfo>();
        private readonly object _closeLock = new object();

        private ILogger _logger;

        /// <inheritdoc />
        public event EventHandler<ManualSelectionEventArgs> OnManualSelection;

        /// <inheritdoc />
        public bool AllowManualSelection { get; set; } = true;

        /// <inheritdoc />
        public IReadOnlyList<PluginLoadError> LoadErrors { get; }

        public IProgressContext Progress { get; set; } = new ProgressContext(new NullProgressOutput());

        public IDialogManager DialogManager { get; set; } = new DefaultDialogManager();

        public ILogger Logger
        {
            get => _logger;
            set => SetLogger(value);
        }

        #region Constructors

        /// <summary>
        /// Creates a new instance of <see cref="PluginManager"/>.
        /// </summary>
        /// <param name="pluginPaths">The paths to search for plugins.</param>
        public PluginManager(params string[] pluginPaths)
        {
            // 1. Setup all necessary instances
            _filePluginLoaders = new IPluginLoader<IFilePlugin>[] { new CsFilePluginLoader(pluginPaths) };
            _gameAdapterLoaders = new IPluginLoader<IGameAdapter>[] { new CsGamePluginLoader(pluginPaths) };

            LoadErrors = _filePluginLoaders.SelectMany(pl => pl.LoadErrors ?? Array.Empty<PluginLoadError>())
                .Concat(_gameAdapterLoaders.SelectMany(pl => pl.LoadErrors ?? Array.Empty<PluginLoadError>()))
                .DistinctBy(e => e.AssemblyPath)
                .ToList();

            _streamMonitor = new StreamMonitor();

            _fileLoader = new FileLoader(_filePluginLoaders);
            _fileSaver = new FileSaver(_streamMonitor);

            _fileLoader.OnManualSelection += FileLoader_OnManualSelection;
        }

        /// <summary>
        /// Creates a new instance of <see cref="PluginManager"/>.
        /// </summary>
        /// <param name="pluginLoaders">The plugin loaders for this manager.</param>
        public PluginManager(params IPluginLoader[] pluginLoaders)
        {
            _filePluginLoaders = pluginLoaders.Where(x => x is IPluginLoader<IFilePlugin>).Cast<IPluginLoader<IFilePlugin>>().ToArray();
            _gameAdapterLoaders = pluginLoaders.Where(x => x is IPluginLoader<IGameAdapter>).Cast<IPluginLoader<IGameAdapter>>().ToArray();

            LoadErrors = pluginLoaders.SelectMany(pl => pl.LoadErrors ?? Array.Empty<PluginLoadError>())
                .DistinctBy(e => e.AssemblyPath)
                .ToList();

            _streamMonitor = new StreamMonitor();

            _fileLoader = new FileLoader(_filePluginLoaders);
            _fileSaver = new FileSaver(_streamMonitor);

            _fileLoader.OnManualSelection += FileLoader_OnManualSelection;
        }

        /// <summary>
        /// Internal constructor for testing.
        /// </summary>
        /// <param name="pluginLoaders">The plugin loaders for this instance.</param>
        /// <param name="fileLoader">The file loader for this instance.</param>
        /// <param name="fileSaver">The file saver for this instance.</param>
        internal PluginManager(IPluginLoader[] pluginLoaders, IFileLoader fileLoader, IFileSaver fileSaver)
        {
            _filePluginLoaders = pluginLoaders.Where(x => x is IPluginLoader<IFilePlugin>)
                .Cast<IPluginLoader<IFilePlugin>>().ToArray();

            _fileLoader = fileLoader;
            _fileSaver = fileSaver;
        }

        #endregion

        #region Get Methods

        /// <inheritdoc />
        public IStateInfo GetLoadedFile(UPath filePath)
        {
            lock (_loadedFilesLock)
            {
                return _loadedFiles.FirstOrDefault(x => UPath.Combine(x.AbsoluteDirectory, x.FilePath.ToRelative()) == filePath);
            }
        }

        /// <inheritdoc />
        public IPluginLoader<IFilePlugin>[] GetFilePluginLoaders()
        {
            return _filePluginLoaders;
        }

        /// <inheritdoc />
        public IPluginLoader<IGameAdapter>[] GetGamePluginLoaders()
        {
            return _gameAdapterLoaders;
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
        public bool IsSaving(IStateInfo stateInfo)
        {
            lock (_saveLock)
            {
                return _savingStates.Contains(stateInfo);
            }
        }

        /// <inheritdoc />
        public bool IsClosing(IStateInfo stateInfo)
        {
            lock (_closeLock)
            {
                return _closingStates.Contains(stateInfo);
            }
        }

        #endregion

        #region Load File

        #region Load Physical

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(string file)
        {
            return LoadFile(file, new LoadFileContext());
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(string file, Guid pluginId)
        {
            return LoadFile(file, new LoadFileContext { PluginId = pluginId });
        }

        /// <inheritdoc />
        public async Task<LoadResult> LoadFile(string file, LoadFileContext loadFileContext)
        {
            // 1. Get UPath
            var path = new UPath(file);

            // If file is already loaded or loading
            lock (_loadingLock)
            {
                if (_loadingFiles.Any(x => x == file))
                    return new LoadResult(false, $"File {file} is already loading.");

                if (IsLoaded(path))
                    return new LoadResult(GetLoadedFile(path));

                _loadingFiles.Add(file);
            }

            // 2. Create file system action
            var fileSystemAction = new Func<IStreamManager, IFileSystem>(streamManager =>
                FileSystemFactory.CreatePhysicalFileSystem(path.GetDirectory(), streamManager));

            // 3. Load file
            // Physical files don't have a parent, if loaded like this
            var loadedFile = await LoadFile(fileSystemAction, path.GetName(), null, loadFileContext);

            lock (_loadingLock)
                _loadingFiles.Remove(file);

            return loadedFile;
        }

        #endregion

        #region Load ArchiveFileInfo

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IStateInfo stateInfo, IArchiveFileInfo afi)
        {
            return LoadFile(stateInfo, afi, new LoadFileContext());
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IStateInfo stateInfo, IArchiveFileInfo afi, Guid pluginId)
        {
            return LoadFile(stateInfo, afi, new LoadFileContext { PluginId = pluginId });
        }

        /// <inheritdoc />
        public async Task<LoadResult> LoadFile(IStateInfo stateInfo, IArchiveFileInfo afi, LoadFileContext loadFileContext)
        {
            // If stateInfo is no archive state
            if (!(stateInfo.PluginState is IArchiveState _))
                throw new InvalidOperationException("The state represents no archive.");

            // If file is already loaded or loading
            var absoluteFilePath = UPath.Combine(stateInfo.AbsoluteDirectory, stateInfo.FilePath.ToRelative(), afi.FilePath.ToRelative());
            lock (_loadingLock)
            {
                if (_loadingFiles.Any(x => x == absoluteFilePath))
                    return new LoadResult(false, $"File {absoluteFilePath} is already loading.");

                if (IsLoaded(absoluteFilePath))
                    return new LoadResult(GetLoadedFile(absoluteFilePath));

                _loadingFiles.Add(absoluteFilePath);
            }

            // 1. Create file system action
            var fileSystemAction = new Func<IStreamManager, IFileSystem>(streamManager =>
                  FileSystemFactory.CreateAfiFileSystem(stateInfo, UPath.Root, streamManager));

            // 2. Load file
            // IArchiveFileInfos have stateInfo as their parent, if loaded like this
            var loadResult = await LoadFile(fileSystemAction, afi.FilePath, stateInfo, loadFileContext);
            if (!loadResult.IsSuccessful)
            {
                lock (_loadingLock)
                    _loadingFiles.Remove(absoluteFilePath);

                return loadResult;
            }

            // 3. Add archive child to parent
            // ArchiveChildren are only added, if a file is loaded like this
            stateInfo.ArchiveChildren.Add(loadResult.LoadedState);

            lock (_loadingLock)
                _loadingFiles.Remove(absoluteFilePath);

            return loadResult;
        }

        #endregion

        #region Load FileSystem

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path)
        {
            return LoadFile(fileSystem, path, null, new LoadFileContext());
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, Guid pluginId)
        {
            return LoadFile(fileSystem, path, null, new LoadFileContext { PluginId = pluginId });
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, IStateInfo parentStateInfo)
        {
            return LoadFile(fileSystem, path, parentStateInfo, new LoadFileContext());
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, Guid pluginId, IStateInfo parentStateInfo)
        {
            return LoadFile(fileSystem, path, parentStateInfo, new LoadFileContext { PluginId = pluginId });
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, LoadFileContext loadFileContext)
        {
            return LoadFile(fileSystem, path, null, loadFileContext);
        }

        /// <inheritdoc />
        public async Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, IStateInfo parentStateInfo, LoadFileContext loadFileContext)
        {
            // Downside of not having ArchiveChildren is not having the states saved below automatically when opened file is saved

            // If file is loaded
            var absoluteFilePath = UPath.Combine(fileSystem.ConvertPathToInternal(UPath.Root), path.ToRelative());
            lock (_loadingLock)
            {
                if (_loadingFiles.Any(x => x == absoluteFilePath))
                    return new LoadResult(false, $"File {absoluteFilePath} is already loading.");

                if (IsLoaded(absoluteFilePath))
                    return new LoadResult(GetLoadedFile(absoluteFilePath));

                _loadingFiles.Add(absoluteFilePath);
            }

            // 1. Create file system action
            var fileSystemAction = new Func<IStreamManager, IFileSystem>(fileSystem.Clone);

            // 2. Load file
            // Only if called by a SubPluginManager the parent state is not null
            // Does not add ArchiveChildren to parent state
            var loadedFile = await LoadFile(fileSystemAction, path, parentStateInfo, loadFileContext);

            lock (_loadingLock)
                _loadingFiles.Remove(absoluteFilePath);

            return loadedFile;
        }

        #endregion

        #region Load Stream

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(StreamFile streamFile)
        {
            return LoadFile(streamFile, new LoadFileContext());
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(StreamFile streamFile, Guid pluginId)
        {
            return LoadFile(streamFile, new LoadFileContext { PluginId = pluginId });
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(StreamFile streamFile, LoadFileContext loadFileContext)
        {
            // We don't check for an already loaded file here, since that should never happen

            // 1. Create file system action
            var fileSystemAction = new Func<IStreamManager, IFileSystem>(streamManager =>
                FileSystemFactory.CreateMemoryFileSystem(streamFile, streamManager));

            // 2. Load file
            // A stream has no parent, since it should never occur to be loaded from somewhere deeper in the system
            return LoadFile(fileSystemAction, streamFile.Path.ToAbsolute(), null, loadFileContext);
        }

        #endregion

        private async Task<LoadResult> LoadFile(Func<IStreamManager, IFileSystem> fileSystemAction, UPath path, IStateInfo parentStateInfo, LoadFileContext loadFileContext)
        {
            // 1. Create stream manager
            var streamManager = _streamMonitor.CreateStreamManager();

            // 2. Create file system
            var fileSystem = fileSystemAction(streamManager);

            // 3. Find plugin
            IFilePlugin plugin = null;
            if (loadFileContext.PluginId != Guid.Empty)
                plugin = _filePluginLoaders.Select(pl => pl.GetPlugin(loadFileContext.PluginId)).First();

            var isRunning = Progress.IsRunning();
            if (!isRunning) Progress.StartProgress();

            // 4. Load file
            var loadResult = await _fileLoader.LoadAsync(fileSystem, path, new LoadInfo
            {
                ParentStateInfo = parentStateInfo,
                StreamManager = streamManager,
                PluginManager = this,
                Plugin = plugin,
                Progress = Progress,
                DialogManager = new InternalDialogManager(DialogManager, loadFileContext.Options),
                AllowManualSelection = AllowManualSelection,
                Logger = loadFileContext.Logger ?? Logger
            });

            if (!isRunning) Progress.FinishProgress();

            // 5. Add file to loaded files
            lock (_loadedFilesLock)
                if (loadResult.IsSuccessful)
                    _loadedFiles.Add(loadResult.LoadedState);

            return loadResult;
        }

        #endregion

        #region Save File

        /// <inheritdoc />
        public Task<SaveResult> SaveFile(IStateInfo stateInfo)
        {
            return SaveFile(stateInfo, stateInfo.FilePath);
        }

        // TODO: Put in options from external call like in Load
        /// <inheritdoc />
        public async Task<SaveResult> SaveFile(IStateInfo stateInfo, IFileSystem fileSystem, UPath savePath)
        {
            if (stateInfo.IsDisposed)
                return new SaveResult(false, "The given file is already closed.");

            lock (_saveLock)
            {
                if (_savingStates.Contains(stateInfo))
                    return new SaveResult(false, $"File {stateInfo.AbsoluteDirectory / stateInfo.FilePath.ToRelative()} is already saving.");

                if (IsClosing(stateInfo))
                    return new SaveResult(false, $"File {stateInfo.AbsoluteDirectory / stateInfo.FilePath.ToRelative()} is currently closing.");

                _savingStates.Add(stateInfo);
            }

            lock (_loadedFilesLock)
                if (!_loadedFiles.Contains(stateInfo))
                    return new SaveResult(false, "The given file is not loaded anymore.");

            var isRunning = Progress.IsRunning();
            if (!isRunning) Progress.StartProgress();

            var saveResult = await _fileSaver.SaveAsync(stateInfo, fileSystem, savePath, new SaveInfo
            {
                Progress = Progress,
                DialogManager = DialogManager,
                Logger = Logger
            });

            if (!isRunning) Progress.FinishProgress();

            lock (_saveLock)
                if (saveResult.IsSuccessful)
                    _savingStates.Remove(stateInfo);

            return saveResult;
        }

        /// <inheritdoc />
        public async Task<SaveResult> SaveFile(IStateInfo stateInfo, UPath saveName)
        {
            if (stateInfo.IsDisposed)
                return new SaveResult(false, "The given file is already closed.");

            lock (_saveLock)
            {
                if (_savingStates.Contains(stateInfo))
                    return new SaveResult(false, $"File {stateInfo.AbsoluteDirectory / stateInfo.FilePath.ToRelative()} is already saving.");

                if (IsClosing(stateInfo))
                    return new SaveResult(false, $"File {stateInfo.AbsoluteDirectory / stateInfo.FilePath.ToRelative()} is currently closing.");

                _savingStates.Add(stateInfo);
            }

            lock (_loadedFilesLock)
                if (!_loadedFiles.Contains(stateInfo))
                    return new SaveResult(false, "The given file is not loaded anymore.");

            var isRunning = Progress.IsRunning();
            if (!isRunning) Progress.StartProgress();

            var saveResult = await _fileSaver.SaveAsync(stateInfo, saveName, new SaveInfo
            {
                Progress = Progress,
                DialogManager = DialogManager,
                Logger = Logger
            });

            if (!isRunning) Progress.FinishProgress();

            lock (_saveLock)
                if (saveResult.IsSuccessful)
                    _savingStates.Remove(stateInfo);

            return saveResult;
        }

        #endregion

        #region Save Stream

        public async Task<SaveStreamResult> SaveStream(IStateInfo stateInfo)
        {
            if (stateInfo.IsDisposed)
                return new SaveStreamResult(false, "The given file is already closed.");

            lock (_saveLock)
            {
                if (_savingStates.Contains(stateInfo))
                    return new SaveStreamResult(false, $"File {stateInfo.AbsoluteDirectory / stateInfo.FilePath.ToRelative()} is already saving.");

                if (IsClosing(stateInfo))
                    return new SaveStreamResult(false, $"File {stateInfo.AbsoluteDirectory / stateInfo.FilePath.ToRelative()} is currently closing.");

                _savingStates.Add(stateInfo);
            }

            lock (_loadedFilesLock)
                if (!_loadedFiles.Contains(stateInfo))
                    return new SaveStreamResult(false, "The given file is not loaded anymore.");

            var isRunning = Progress.IsRunning();
            if (!isRunning) Progress.StartProgress();

            // Save to memory file system
            var fileSystem = new MemoryFileSystem(stateInfo.StreamManager);
            var saveResult = await _fileSaver.SaveAsync(stateInfo, fileSystem, stateInfo.FilePath, new SaveInfo
            {
                Progress = Progress,
                DialogManager = DialogManager,
                Logger = Logger
            });

            if (!isRunning) Progress.FinishProgress();

            lock (_saveLock)
                if (saveResult.IsSuccessful)
                    _savingStates.Remove(stateInfo);

            if (!saveResult.IsSuccessful)
                return new SaveStreamResult(saveResult.Exception);

            // Collect all StreamFiles from memory file system
            var streamFiles = fileSystem.EnumerateAllFiles(UPath.Root).Select(x =>
                new StreamFile(fileSystem.OpenFile(x, FileMode.Open, FileAccess.Read, FileShare.Read), x)).ToArray();

            return new SaveStreamResult(streamFiles);
        }

        #endregion

        #region Close File

        /// <inheritdoc />
        public CloseResult Close(IStateInfo stateInfo)
        {
            if (stateInfo.IsDisposed)
                return CloseResult.SuccessfulResult;

            lock (_closeLock)
            {
                if (_closingStates.Contains(stateInfo))
                    return new CloseResult(false, $"File {stateInfo.AbsoluteDirectory / stateInfo.FilePath.ToRelative()} is already closing.");

                if (IsSaving(stateInfo))
                    return new CloseResult(false, $"File {stateInfo.AbsoluteDirectory / stateInfo.FilePath.ToRelative()} is currently saving.");

                _closingStates.Add(stateInfo);
            }

            lock (_loadedFilesLock)
                if (!_loadedFiles.Contains(stateInfo))
                    return new CloseResult(false, "The given file is not loaded anymore.");

            // Remove state from its parent
            stateInfo.ParentStateInfo?.ArchiveChildren.Remove(stateInfo);

            CloseInternal(stateInfo);

            lock (_closeLock)
                _closingStates.Remove(stateInfo);

            return CloseResult.SuccessfulResult;
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

        private void CloseInternal(IStateInfo stateInfo)
        {
            ContractAssertions.IsNotNull(stateInfo, nameof(stateInfo));

            // Close children of this state first
            foreach (var child in stateInfo.ArchiveChildren)
                CloseInternal(child);

            // Close indirect children of this state
            // Indirect children occur when a file is loaded by a FileSystem and got a parent attached manually
            IList<IStateInfo> indirectChildren;
            lock (_loadedFilesLock)
                indirectChildren = _loadedFiles.Where(x => x.ParentStateInfo == stateInfo).ToArray();

            foreach (var indirectChild in indirectChildren)
                CloseInternal(indirectChild);

            lock (_loadedFilesLock)
            {
                // Close state itself
                if (_streamMonitor.Manages(stateInfo.StreamManager))
                    _streamMonitor.RemoveStreamManager(stateInfo.StreamManager);
                stateInfo.Dispose();

                // Remove from the file tracking of this instance
                _loadedFiles.Remove(stateInfo);
            }
        }

        #endregion

        public void Dispose()
        {
            CloseAll();

            _streamMonitor?.Dispose();
        }

        private void FileLoader_OnManualSelection(object sender, ManualSelectionEventArgs e)
        {
            OnManualSelection?.Invoke(sender, e);
        }

        private void SetLogger(ILogger logger)
        {
            _logger = logger;
            _streamMonitor.Logger = logger;
        }
    }
}
