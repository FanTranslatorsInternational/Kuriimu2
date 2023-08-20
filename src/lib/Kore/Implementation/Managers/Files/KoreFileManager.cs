using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kontract;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers.Dialogs;
using Kontract.Interfaces.Managers.Files;
using Kontract.Interfaces.Managers.Streams;
using Kontract.Interfaces.Plugins.Entry;
using Kontract.Interfaces.Plugins.Loaders;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Interfaces.Progress;
using Kontract.Models.FileSystem;
using Kontract.Models.Managers.Files;
using Kontract.Models.Plugins.Entry;
using Kontract.Models.Plugins.Loaders;
using Kore.Factories;
using Kore.Implementation.FileSystem;
using Kore.Implementation.Managers.Dialogs;
using Kore.Implementation.Managers.Files.Support;
using Kore.Implementation.Managers.Streams;
using Kore.Implementation.Plugins.Loaders;
using Kore.Implementation.Progress;
using Kore.Models.Managers.Files;
using Kore.Models.Managers.Files.Support;
using Kore.Models.UnsupportedPlugin;
using MoreLinq;
using Serilog;

namespace Kore.Implementation.Managers.Files
{
    /// <summary>
    /// The core component of the Kuriimu runtime.
    /// </summary>
    public class KoreFileManager : IKoreFileManager
    {
        private readonly IPluginLoader<IFilePlugin>[] _filePluginLoaders;
        //private readonly IPluginLoader<IGameAdapter>[] _gameAdapterLoaders;

        private readonly FileLoader _fileLoader;
        private readonly FileSaver _fileSaver;

        private readonly StreamMonitor _streamMonitor;

        private readonly IList<UPath> _loadingFiles = new List<UPath>();
        private readonly object _loadingLock = new object();

        private readonly IList<IFileState> _loadedFiles = new List<IFileState>();
        private readonly object _loadedFilesLock = new object();

        private readonly IList<IFileState> _savingStates = new List<IFileState>();
        private readonly object _saveLock = new object();

        private readonly IList<IFileState> _closingStates = new List<IFileState>();
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
        /// Creates a new instance of <see cref="KoreFileManager"/>.
        /// </summary>
        /// <param name="pluginPaths">The paths to search for plugins.</param>
        public KoreFileManager(params string[] pluginPaths) :
            this(
                new FilePluginLoader(pluginPaths)//, new CsGamePluginLoader(pluginPaths)
            )
        { }

        /// <summary>
        /// Creates a new instance of <see cref="KoreFileManager"/>.
        /// </summary>
        /// <param name="pluginLoaders">The plugin loaders for this manager.</param>
        public KoreFileManager(params IPluginLoader[] pluginLoaders)
        {
            _filePluginLoaders = pluginLoaders.Where(x => x is IPluginLoader<IFilePlugin>).Cast<IPluginLoader<IFilePlugin>>().ToArray();
            //_gameAdapterLoaders = pluginLoaders.Where(x => x is IPluginLoader<IGameAdapter>).Cast<IPluginLoader<IGameAdapter>>().ToArray();

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
        internal KoreFileManager(IPluginLoader[] pluginLoaders, FileLoader fileLoader, FileSaver fileSaver)
        {
            _filePluginLoaders = pluginLoaders.Where(x => x is IPluginLoader<IFilePlugin>)
                .Cast<IPluginLoader<IFilePlugin>>().ToArray();

            _fileLoader = fileLoader;
            _fileSaver = fileSaver;
        }

        #endregion

        #region Get Methods

        /// <inheritdoc />
        public IFileState GetLoadedFile(UPath filePath)
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
        //public IPluginLoader<IGameAdapter>[] GetGamePluginLoaders()
        //{
        //    return _gameAdapterLoaders;
        //}

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

        public Task<bool> CanIdentify(string file, Guid pluginId)
        {
            // 1. Create file system
            var streamManager = CreateStreamManager();
            var fileSystem = FileSystemFactory.CreatePhysicalFileSystem(streamManager);
            var filePath = fileSystem.ConvertPathFromInternal(file);

            var root = filePath.GetRoot();
            fileSystem = FileSystemFactory.CreateSubFileSystem(fileSystem, root);

            // 2. Identify file
            return CanIdentify(fileSystem, filePath.GetSubDirectory(root), streamManager, pluginId);
        }

        public Task<bool> CanIdentify(IFileState fileState, IArchiveFileInfo afi, Guid pluginId)
        {
            // 1. Create file system
            var streamManager = CreateStreamManager();
            var fileSystem = FileSystemFactory.CreateAfiFileSystem(fileState, UPath.Root, streamManager);

            // 2. Identify file
            return CanIdentify(fileSystem, afi.FilePath, streamManager, pluginId);
        }

        public Task<bool> CanIdentify(StreamFile streamFile, Guid pluginId)
        {
            // 1. Create file system
            var streamManager = CreateStreamManager();
            var fileSystem = FileSystemFactory.CreateMemoryFileSystem(streamFile, streamManager);

            // 2. Identify file
            return CanIdentify(fileSystem, streamFile.Path.ToAbsolute(), streamManager, pluginId);
        }

        public Task<bool> CanIdentify(IFileSystem fileSystem, UPath path, Guid pluginId)
        {
            // 1. Create controlled file system
            var streamManager = CreateStreamManager();
            var clonedFileSystem = fileSystem.Clone(streamManager);

            // 2. Identify file
            return CanIdentify(clonedFileSystem, path, streamManager, pluginId);
        }

        private async Task<bool> CanIdentify(IFileSystem fileSystem, UPath path, IStreamManager streamManager, Guid pluginId)
        {
            // 1. Get plugin
            var plugin = GetFilePlugin(pluginId);
            if (plugin == null)
                return false;

            // 2. If plugin cannot identify
            if (!plugin.CanIdentifyFiles)
                return false;

            // 3. Identify file by plugin
            var identifyContext = new IdentifyContext(streamManager.CreateTemporaryStreamProvider());
            var result = await (plugin as IIdentifyFiles).IdentifyAsync(fileSystem, path, identifyContext);

            // 4. Clean up
            streamManager.ReleaseAll();

            return result;
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
            var logger = loadFileContext.Logger ?? Logger;
            logger.Information("Load Physical: {0}", file);

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
                {
                    logger.Error("Load Physical: Loading");
                    return new KoreLoadResult(LoadErrorReason.Loading);
                }

                if (IsLoaded(file))
                {
                    logger.Information("Load Physical: Done");
                    return new KoreLoadResult(GetLoadedFile(file));
                }

                _loadingFiles.Add(file);
            }

            // 3. Load file
            // Physical files don't have a parent, if loaded like this
            var loadedFile = await LoadFile(fileSystem, filePath.GetSubDirectory(root), streamManager, null, loadFileContext);

            lock (_loadingLock)
                _loadingFiles.Remove(file);

            if (loadedFile.Reason == LoadErrorReason.None)
                logger.Information("Load Physical: Done");
            else
                logger.Error("Load Physical: {0}", loadedFile.Reason.ToString());

            return loadedFile;
        }

        #endregion

        #region Load ArchiveFileInfo

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IFileState fileState, IArchiveFileInfo afi)
        {
            return LoadFile(fileState, afi, new LoadFileContext());
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IFileState fileState, IArchiveFileInfo afi, Guid pluginId)
        {
            return LoadFile(fileState, afi, new LoadFileContext { PluginId = pluginId });
        }

        /// <inheritdoc />
        public async Task<LoadResult> LoadFile(IFileState fileState, IArchiveFileInfo afi, LoadFileContext loadFileContext)
        {
            var logger = loadFileContext.Logger ?? Logger;
            logger.Information("Load ArchiveFileInfo: {0}", afi.FilePath);

            // If fileState is no archive state
            if (!(fileState.PluginState is IArchiveState))
            {
                logger.Error("Load ArchiveFileInfo: No archive plugin");
                return new KoreLoadResult(LoadErrorReason.NoArchive);
            }

            // If file is already loaded or loading
            var absoluteFilePath = UPath.Combine(fileState.AbsoluteDirectory, fileState.FilePath.ToRelative(), afi.FilePath.ToRelative());
            lock (_loadingLock)
            {
                if (_loadingFiles.Any(x => x == absoluteFilePath))
                {
                    logger.Error("Load ArchiveFileInfo: Loading");
                    return new KoreLoadResult(LoadErrorReason.Loading);
                }

                if (IsLoaded(absoluteFilePath))
                {
                    logger.Information("Load ArchiveFileInfo: Done");
                    return new KoreLoadResult(GetLoadedFile(absoluteFilePath));
                }

                _loadingFiles.Add(absoluteFilePath);
            }

            // 1. Create file system
            var streamManager = CreateStreamManager();
            var fileSystem = FileSystemFactory.CreateAfiFileSystem(fileState, UPath.Root, streamManager);

            // 2. Load file
            // IArchiveFileInfos have fileState as their parent, if loaded like this
            var loadResult = await LoadFile(fileSystem, afi.FilePath, streamManager, fileState, loadFileContext);
            if (!loadResult.IsSuccessful)
            {
                lock (_loadingLock)
                    _loadingFiles.Remove(absoluteFilePath);

                logger.Error("Load ArchiveFileInfo: {0}", loadResult.Reason.ToString());
                return loadResult;
            }

            // 3. Add archive child to parent
            // ArchiveChildren are only added, if a file is loaded like this
            fileState.ArchiveChildren.Add(loadResult.LoadedFileState);

            lock (_loadingLock)
                _loadingFiles.Remove(absoluteFilePath);

            logger.Information("Load ArchiveFileInfo: Done");

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
        public Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, IFileState parentFileState)
        {
            return LoadFile(fileSystem, path, parentFileState, new LoadFileContext());
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, Guid pluginId, IFileState parentFileState)
        {
            return LoadFile(fileSystem, path, parentFileState, new LoadFileContext { PluginId = pluginId });
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, LoadFileContext loadFileContext)
        {
            return LoadFile(fileSystem, path, null, loadFileContext);
        }

        /// <inheritdoc />
        public async Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, IFileState parentFileState, LoadFileContext loadFileContext)
        {
            // Downside of not having ArchiveChildren is not having the states saved below automatically when opened file is saved

            var logger = loadFileContext.Logger ?? Logger;
            logger.Information("Load FileSystem: {0}", path);

            // If file is loaded
            var absoluteFilePath = UPath.Combine(fileSystem.ConvertPathToInternal(UPath.Root), path.ToRelative());
            lock (_loadingLock)
            {
                if (_loadingFiles.Any(x => x == absoluteFilePath))
                {
                    logger.Error("Load FileSystem: Loading");
                    return new KoreLoadResult(LoadErrorReason.Loading);
                }

                if (IsLoaded(absoluteFilePath))
                {
                    logger.Information("Load FileSystem: Done");
                    return new KoreLoadResult(GetLoadedFile(absoluteFilePath));
                }

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

            if (loadedFile.Reason == LoadErrorReason.None)
                logger.Information("Load FileSystem: Done");
            else
                logger.Error("Load FileSystem: {0}", loadedFile.Reason.ToString());

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
        public async Task<LoadResult> LoadFile(StreamFile streamFile, LoadFileContext loadFileContext)
        {
            // We don't check for an already loaded file here, since that should never happen

            var logger = loadFileContext.Logger ?? Logger;
            logger.Information("Load Stream: {0}", streamFile.Path);

            // 1. Create file system action
            var streamManager = CreateStreamManager();
            var fileSystem = FileSystemFactory.CreateMemoryFileSystem(streamFile, streamManager);

            // 2. Load file
            // A stream has no parent, since it should never occur to be loaded from somewhere deeper in the system
            var loadResult = await LoadFile(fileSystem, streamFile.Path.ToAbsolute(), streamManager, null, loadFileContext);

            if (loadResult.Reason == LoadErrorReason.None)
                logger.Information("Load Stream: Done");
            else
                logger.Error("Load Stream: {0}", loadResult.Reason.ToString());

            return loadResult;
        }

        #endregion

        private async Task<KoreLoadResult> LoadFile(IFileSystem fileSystem, UPath path, IStreamManager streamManager, IFileState parentFileState, LoadFileContext loadFileContext)
        {
            // 1. Find plugin
            IFilePlugin plugin = null;
            if (loadFileContext.PluginId == RawPlugin.Guid)
                plugin = new RawPlugin();
            else if (loadFileContext.PluginId != Guid.Empty)
                plugin = _filePluginLoaders.Select(pl => pl.GetPlugin(loadFileContext.PluginId)).First();

            var isRunning = Progress.IsRunning();
            if (!isRunning) Progress.StartProgress();

            // 2. Load file
            var loadResult = await _fileLoader.LoadAsync(fileSystem, path, new LoadInfo
            {
                ParentFileState = parentFileState,
                StreamManager = streamManager,
                KoreFileManager = this,
                Plugin = plugin,
                Progress = Progress,
                DialogManager = new PredefinedDialogManager(DialogManager, loadFileContext.Options),
                AllowManualSelection = AllowManualSelection,
                Logger = loadFileContext.Logger ?? Logger
            });

            if (!isRunning) Progress.FinishProgress();

            // 5. Add file to loaded files
            lock (_loadedFilesLock)
                if (loadResult.IsSuccessful)
                    _loadedFiles.Add(loadResult.LoadedFileState);

            return loadResult;
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
            Logger.Information("Save File: {0} -> {1}", fileState.AbsoluteDirectory / fileState.FilePath.ToRelative(), savePath);

            if (fileState.IsDisposed)
            {
                Logger.Error("Save File: Closed");
                return new KoreSaveResult(SaveErrorReason.Closed);
            }

            lock (_saveLock)
            {
                if (_savingStates.Contains(fileState))
                {
                    Logger.Error("Save File: Saving");
                    return new KoreSaveResult(SaveErrorReason.Saving);
                }

                if (IsClosing(fileState))
                {
                    Logger.Error("Save File: Closing");
                    return new KoreSaveResult(SaveErrorReason.Closing);
                }

                _savingStates.Add(fileState);
            }

            lock (_loadedFilesLock)
                if (!_loadedFiles.Contains(fileState))
                {
                    Logger.Error("Save File: Not loaded");
                    return new KoreSaveResult(SaveErrorReason.NotLoaded);
                }

            var isRunning = Progress.IsRunning();
            if (!isRunning) Progress.StartProgress();

            var saveResult = await _fileSaver.SaveAsync(fileState, fileSystem, savePath, new SaveInfo
            {
                Progress = Progress,
                DialogManager = DialogManager,
                Logger = Logger
            });

            if (!isRunning) Progress.FinishProgress();

            lock (_saveLock)
                _savingStates.Remove(fileState);

            if (saveResult.Reason == SaveErrorReason.None)
                Logger.Information("Save File: Done");
            else
                Logger.Error("Save File: {0}", saveResult.Reason.ToString());

            return saveResult;
        }

        #endregion

        #region Save Stream

        public async Task<SaveStreamResult> SaveStream(IFileState fileState)
        {
            Logger.Information("Save Stream: {0}", fileState.AbsoluteDirectory / fileState.FilePath.ToRelative());

            if (fileState.IsDisposed)
            {
                Logger.Error("Save Stream: Closed");
                return new KoreSaveStreamResult(SaveErrorReason.Closed);
            }

            lock (_saveLock)
            {
                if (_savingStates.Contains(fileState))
                {
                    Logger.Error("Save Stream: Saving");
                    return new KoreSaveStreamResult(SaveErrorReason.Saving);
                }

                if (IsClosing(fileState))
                {
                    Logger.Error("Save Stream: Closing");
                    return new KoreSaveStreamResult(SaveErrorReason.Closing);
                }

                _savingStates.Add(fileState);
            }

            lock (_loadedFilesLock)
                if (!_loadedFiles.Contains(fileState))
                {
                    Logger.Error("Save Stream: Not loaded");
                    return new KoreSaveStreamResult(SaveErrorReason.NotLoaded);
                }

            var isRunning = Progress.IsRunning();
            if (!isRunning) Progress.StartProgress();

            // Save to memory file system
            var fileSystem = new MemoryFileSystem(fileState.StreamManager);
            var saveResult = await _fileSaver.SaveAsync(fileState, fileSystem, fileState.FilePath, new SaveInfo
            {
                Progress = Progress,
                DialogManager = DialogManager,
                Logger = Logger
            });

            if (!isRunning) Progress.FinishProgress();

            lock (_saveLock)
                _savingStates.Remove(fileState);

            if (!saveResult.IsSuccessful)
            {
                Logger.Error("Save Stream: {0}", saveResult.Reason.ToString());
                return new KoreSaveStreamResult(saveResult.Reason, saveResult.Exception);
            }

            // Collect all StreamFiles from memory file system
            var streamFiles = fileSystem.EnumerateAllFiles(UPath.Root).Select(x =>
                new StreamFile(fileSystem.OpenFile(x, FileMode.Open, FileAccess.Read, FileShare.Read), x)).ToArray();

            Logger.Information("Save Stream: Done");

            return new SaveStreamResult(streamFiles);
        }

        #endregion

        #region Create file


        #endregion

        #region Close File

        /// <inheritdoc />
        public CloseResult Close(IFileState fileState)
        {
            Logger.Information("Close: {0}", fileState.AbsoluteDirectory / fileState.FilePath.ToRelative());

            if (fileState.IsDisposed)
            {
                Logger.Information("Close: Done");
                return KoreCloseResult.Success;
            }

            lock (_closeLock)
            {
                if (_closingStates.Contains(fileState))
                {
                    Logger.Error("Close: Closing");
                    return new KoreCloseResult(CloseErrorReason.Closing);
                }

                if (IsSaving(fileState))
                {
                    Logger.Error("Close: Saving");
                    return new KoreCloseResult(CloseErrorReason.Saving);
                }

                _closingStates.Add(fileState);
            }

            lock (_loadedFilesLock)
                if (!_loadedFiles.Contains(fileState))
                {
                    Logger.Error("Close: Not Loaded");
                    return new KoreCloseResult(CloseErrorReason.NotLoaded);
                }

            // Remove state from its parent
            fileState.ParentFileState?.ArchiveChildren.Remove(fileState);

            CloseInternal(fileState);

            lock (_closeLock)
                _closingStates.Remove(fileState);

            Logger.Information("Close: Done");

            return KoreCloseResult.Success;
        }

        /// <inheritdoc />
        public void CloseAll()
        {
            Logger.Information("Close All");

            lock (_loadedFilesLock)
            {
                foreach (var stateInfo in _loadedFiles)
                {
                    lock (_closeLock)
                    {
                        if (_closingStates.Contains(stateInfo))
                        {
                            Logger.Error("Close All: Closing; {0}", stateInfo.AbsoluteDirectory/stateInfo.FilePath.ToRelative());
                            return;
                        }

                        if (IsSaving(stateInfo))
                        {
                            Logger.Error("Close All: Saving; {0}", stateInfo.AbsoluteDirectory / stateInfo.FilePath.ToRelative());
                            return;
                        }

                        _closingStates.Add(stateInfo);
                    }

                    stateInfo.Dispose();

                    lock (_closeLock)
                        _closingStates.Remove(stateInfo);
                }

                _loadedFiles.Clear();
            }

            Logger.Information("Close All: Done");
        }

        private void CloseInternal(IFileState fileState)
        {
            ContractAssertions.IsNotNull(fileState, nameof(fileState));

            // Close children of this state first
            foreach (var child in fileState.ArchiveChildren)
                CloseInternal(child);

            // Close indirect children of this state
            // Indirect children occur when a file is loaded by a FileSystem and got a parent attached manually
            IList<IFileState> indirectChildren;
            lock (_loadedFilesLock)
                indirectChildren = _loadedFiles.Where(x => x.ParentFileState == fileState).ToArray();

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

        private IStreamManager CreateStreamManager()
        {
            return _streamMonitor.CreateStreamManager();
        }

        private IFilePlugin GetFilePlugin(Guid pluginId)
        {
            return GetFilePluginLoaders().SelectMany(x => x.Plugins).FirstOrDefault(x => x.PluginId == pluginId);
        }

        public class ManualSelectionEventArgs : EventArgs
        {
            public IEnumerable<IFilePlugin> FilePlugins { get; }
            public IEnumerable<IFilePlugin> FilteredFilePlugins { get; }
            public SelectionStatus SelectionStatus { get; }

            public IFilePlugin Result { get; set; }

            public ManualSelectionEventArgs(IEnumerable<IFilePlugin> allFilePlugins, IEnumerable<IFilePlugin> filteredFilePlugins, SelectionStatus status)
            {
                FilePlugins = allFilePlugins;
                FilteredFilePlugins = filteredFilePlugins;
                SelectionStatus = status;
            }
        }

        public enum SelectionStatus
        {
            All,
            MultipleMatches,
            NonIdentifiable
        }
    }
}
