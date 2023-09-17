﻿using System;
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
using Serilog;

namespace Kore.Managers.Plugins
{
    /// <summary>
    /// The core component of the Kuriimu runtime.
    /// </summary>
    public class FileManager : IFileManager
    {
        private readonly IPluginLoader<IFilePlugin>[] _filePluginLoaders;
        private readonly IPluginLoader<IGameAdapter>[] _gameAdapterLoaders;

        private readonly IFileLoader _fileLoader;
        private readonly IFileSaver _fileSaver;

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
        /// Creates a new instance of <see cref="FileManager"/>.
        /// </summary>
        /// <param name="pluginPaths">The paths to search for plugins.</param>
        public FileManager(params string[] pluginPaths) :
            this(
                new CsFilePluginLoader(pluginPaths),
                new CsGamePluginLoader(pluginPaths)
            )
        { }

        /// <summary>
        /// Creates a new instance of <see cref="FileManager"/>.
        /// </summary>
        /// <param name="pluginLoaders">The plugin loaders for this manager.</param>
        public FileManager(params IPluginLoader[] pluginLoaders)
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
        internal FileManager(IPluginLoader[] pluginLoaders, IFileLoader fileLoader, IFileSaver fileSaver)
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
                    return new LoadResult(false, $"File {file} is already loading.");

                if (IsLoaded(file))
                    return new LoadResult(GetLoadedFile(file));

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
            // If fileState is no archive state
            if (!(fileState.PluginState is IArchiveState))
                throw new InvalidOperationException("The state represents no archive.");

            // If file is already loaded or loading
            var absoluteFilePath = UPath.Combine(fileState.AbsoluteDirectory, fileState.FilePath.ToRelative(), afi.FilePath.ToRelative());
            lock (_loadingLock)
            {
                if (_loadingFiles.Any(x => x == absoluteFilePath))
                    return new LoadResult(false, $"File {absoluteFilePath} is already loading.");

                if (IsLoaded(absoluteFilePath))
                    return new LoadResult(GetLoadedFile(absoluteFilePath));

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
            var streamManager = CreateStreamManager();
            var fileSystem = FileSystemFactory.CreateMemoryFileSystem(streamFile, streamManager);

            // 2. Load file
            // A stream has no parent, since it should never occur to be loaded from somewhere deeper in the system
            return LoadFile(fileSystem, streamFile.Path.ToAbsolute(), streamManager, null, loadFileContext);
        }

        #endregion

        private async Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, IStreamManager streamManager, IFileState parentFileState, LoadFileContext loadFileContext)
        {
            // 1. Find plugin
            IFilePlugin plugin = null;
            if (loadFileContext.PluginId != Guid.Empty)
                plugin = _filePluginLoaders.Select(pl => pl.GetPlugin(loadFileContext.PluginId)).First();

            var isRunning = Progress.IsRunning();
            if (!isRunning) Progress.StartProgress();

            // 2. Load file
            var loadResult = await _fileLoader.LoadAsync(fileSystem, path, new LoadInfo
            {
                ParentFileState = parentFileState,
                StreamManager = streamManager,
                FileManager = this,
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
            if (fileState.IsDisposed)
                return new SaveResult(false, "The given file is already closed.");

            lock (_saveLock)
            {
                if (_savingStates.Contains(fileState))
                    return new SaveResult(false, $"File {fileState.AbsoluteDirectory / fileState.FilePath.ToRelative()} is already saving.");

                if (IsClosing(fileState))
                    return new SaveResult(false, $"File {fileState.AbsoluteDirectory / fileState.FilePath.ToRelative()} is currently closing.");

                _savingStates.Add(fileState);
            }

            lock (_loadedFilesLock)
                if (!_loadedFiles.Contains(fileState))
                    return new SaveResult(false, "The given file is not loaded anymore.");

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

            return saveResult;
        }

        #endregion

        #region Save Stream

        public async Task<SaveStreamResult> SaveStream(IFileState fileState)
        {
            if (fileState.IsDisposed)
                return new SaveStreamResult(false, "The given file is already closed.");

            lock (_saveLock)
            {
                if (_savingStates.Contains(fileState))
                    return new SaveStreamResult(false, $"File {fileState.AbsoluteDirectory / fileState.FilePath.ToRelative()} is already saving.");

                if (IsClosing(fileState))
                    return new SaveStreamResult(false, $"File {fileState.AbsoluteDirectory / fileState.FilePath.ToRelative()} is currently closing.");

                _savingStates.Add(fileState);
            }

            lock (_loadedFilesLock)
                if (!_loadedFiles.Contains(fileState))
                    return new SaveStreamResult(false, "The given file is not loaded anymore.");

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
                return new SaveStreamResult(saveResult.Exception);

            // Collect all StreamFiles from memory file system
            var streamFiles = fileSystem.EnumerateAllFiles(UPath.Root).Select(x =>
                new StreamFile(fileSystem.OpenFile(x, FileMode.Open, FileAccess.Read, FileShare.Read), x)).ToArray();

            return new SaveStreamResult(streamFiles, saveResult.Message);
        }

        #endregion

        #region Create file


        #endregion

        #region Close File

        /// <inheritdoc />
        public CloseResult Close(IFileState fileState)
        {
            if (fileState.IsDisposed)
                return CloseResult.SuccessfulResult;

            lock (_closeLock)
            {
                if (_closingStates.Contains(fileState))
                    return new CloseResult(false, $"File {fileState.AbsoluteDirectory / fileState.FilePath.ToRelative()} is already closing.");

                if (IsSaving(fileState))
                    return new CloseResult(false, $"File {fileState.AbsoluteDirectory / fileState.FilePath.ToRelative()} is currently saving.");

                _closingStates.Add(fileState);
            }

            lock (_loadedFilesLock)
                if (!_loadedFiles.Contains(fileState))
                    return new CloseResult(false, "The given file is not loaded anymore.");

            // Remove state from its parent
            fileState.ParentFileState?.ArchiveChildren.Remove(fileState);

            CloseInternal(fileState);

            lock (_closeLock)
                _closingStates.Remove(fileState);

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
    }
}
