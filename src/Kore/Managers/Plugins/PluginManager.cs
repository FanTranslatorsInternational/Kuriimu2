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
using Kore.Managers.Plugins.FileManagement;
using Kore.Managers.Plugins.PluginLoader;
using Kore.Models.LoadInfo;
using Kore.Progress;
using MoreLinq;

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

        private readonly IProgressContext _progress;
        private readonly IDialogManager _dialogManager;

        private readonly StreamMonitor _streamMonitor;

        private readonly IList<IStateInfo> _loadedFiles;
        private readonly object _loadedFilesLock = new object();

        /// <inheritdoc />
        public event EventHandler<ManualSelectionEventArgs> OnManualSelection;

        /// <inheritdoc />
        public bool AllowManualSelection { get; set; } = true;

        /// <inheritdoc />
        public IReadOnlyList<PluginLoadError> LoadErrors { get; }

        #region Constructors

        /// <summary>
        /// Creates a new instance of <see cref="PluginManager"/>.
        /// </summary>
        /// <param name="pluginPaths">The paths to search for plugins.</param>
        public PluginManager(params string[] pluginPaths) :
            this(new ProgressContext(new NullProgressOutput()), new DefaultDialogManager(), pluginPaths)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="PluginManager"/>.
        /// </summary>
        /// <param name="progress">The progress context for plugin processes.</param>
        /// <param name="pluginPaths">The paths to search for plugins.</param>
        public PluginManager(IProgressContext progress, params string[] pluginPaths) :
            this(progress, new DefaultDialogManager(), pluginPaths)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="PluginManager"/>.
        /// </summary>
        /// <param name="dialogManager">The dialog manager for plugin processes.</param>
        /// <param name="pluginPaths">The paths to search for plugins.</param>
        public PluginManager(IDialogManager dialogManager, params string[] pluginPaths) :
            this(new ProgressContext(new NullProgressOutput()), dialogManager, pluginPaths)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="PluginManager"/>.
        /// </summary>
        /// <param name="progress">The progress context for plugin processes.</param>
        /// <param name="dialogManager">The dialog manager for plugin processes.</param>
        /// <param name="pluginPaths">The paths to search for plugins.</param>
        public PluginManager(IProgressContext progress, IDialogManager dialogManager, params string[] pluginPaths)
        {
            ContractAssertions.IsNotNull(progress, nameof(progress));
            ContractAssertions.IsNotNull(dialogManager, nameof(dialogManager));

            // 1. Setup all necessary instances
            _filePluginLoaders = new IPluginLoader<IFilePlugin>[] { new CsFilePluginLoader(pluginPaths) };
            _gameAdapterLoaders = new IPluginLoader<IGameAdapter>[] { new CsGamePluginLoader(pluginPaths) };

            _progress = progress;
            _dialogManager = dialogManager;

            LoadErrors = _filePluginLoaders.SelectMany(pl => pl.LoadErrors ?? Array.Empty<PluginLoadError>())
                .Concat(_gameAdapterLoaders.SelectMany(pl => pl.LoadErrors ?? Array.Empty<PluginLoadError>()))
                .DistinctBy(e => e.AssemblyPath)
                .ToList();

            _streamMonitor = new StreamMonitor();

            _fileLoader = new FileLoader(_filePluginLoaders);
            _fileSaver = new FileSaver(_streamMonitor, dialogManager);

            _fileLoader.OnManualSelection += FileLoader_OnManualSelection;

            _loadedFiles = new List<IStateInfo>();
        }

        /// <summary>
        /// Creates a new instance of <see cref="PluginManager"/>.
        /// </summary>
        /// <param name="pluginLoaders">The plugin loaders for this manager.</param>
        public PluginManager(params IPluginLoader[] pluginLoaders) :
            this(new ProgressContext(new NullProgressOutput()), new DefaultDialogManager(), pluginLoaders)
        {
        }

        public PluginManager(IProgressContext progress, params IPluginLoader[] pluginLoaders) :
            this(progress, new DefaultDialogManager(), pluginLoaders)
        {
        }

        public PluginManager(IDialogManager dialogManager, params IPluginLoader[] pluginLoaders) :
            this(new ProgressContext(new NullProgressOutput()), dialogManager, pluginLoaders)
        {
        }

        public PluginManager(IProgressContext progress, IDialogManager dialogManager, params IPluginLoader[] pluginLoaders)
        {
            ContractAssertions.IsNotNull(progress, nameof(progress));
            ContractAssertions.IsNotNull(dialogManager, nameof(dialogManager));

            _filePluginLoaders = pluginLoaders.Where(x => x is IPluginLoader<IFilePlugin>).Cast<IPluginLoader<IFilePlugin>>().ToArray();
            _gameAdapterLoaders = pluginLoaders.Where(x => x is IPluginLoader<IGameAdapter>).Cast<IPluginLoader<IGameAdapter>>().ToArray();

            _progress = progress;
            _dialogManager = dialogManager;

            LoadErrors = pluginLoaders.SelectMany(pl => pl.LoadErrors ?? Array.Empty<PluginLoadError>())
                .DistinctBy(e => e.AssemblyPath)
                .ToList();

            _streamMonitor = new StreamMonitor();

            _fileLoader = new FileLoader(_filePluginLoaders);
            _fileSaver = new FileSaver(_streamMonitor, dialogManager);

            _fileLoader.OnManualSelection += FileLoader_OnManualSelection;

            _loadedFiles = new List<IStateInfo>();
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

            _loadedFiles = new List<IStateInfo>();
        }

        #endregion

        /// <inheritdoc />
        public bool IsLoaded(UPath filePath)
        {
            lock (_loadedFilesLock)
            {
                return _loadedFiles.Any(x => UPath.Combine(x.AbsoluteDirectory, x.FilePath.ToRelative()) == filePath);
            }
        }

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

        #region Load File

        #region Load Physical

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(string file)
        {
            return LoadFile(file, Guid.Empty, new LoadFileContext());
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(string file, LoadFileContext loadFileContext)
        {
            return LoadFile(file, Guid.Empty, loadFileContext);
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(string file, Guid pluginId)
        {
            return LoadFile(file, pluginId, new LoadFileContext());
        }

        public Task<LoadResult> LoadFile(string file, Guid pluginId, LoadFileContext loadFileContext)
        {
            // 1. Get UPath
            var path = new UPath(file);

            // If file is already loaded
            if (IsLoaded(path))
                return Task.FromResult(new LoadResult(GetLoadedFile(path)));

            // 2. Create file system action
            var fileSystemAction = new Func<IStreamManager, IFileSystem>(streamManager =>
                FileSystemFactory.CreatePhysicalFileSystem(path.GetDirectory(), streamManager));

            // 3. Load file
            // Physical files don't have a parent, if loaded like this
            return LoadFile(fileSystemAction, path.GetName(), null, pluginId, loadFileContext);
        }

        #endregion

        #region Load IArchiveFileInfo

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IStateInfo stateInfo, IArchiveFileInfo afi)
        {
            return LoadFile(stateInfo, afi, Guid.Empty, new LoadFileContext());
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IStateInfo stateInfo, IArchiveFileInfo afi, LoadFileContext loadFileContext)
        {
            return LoadFile(stateInfo, afi, Guid.Empty, loadFileContext);
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IStateInfo stateInfo, IArchiveFileInfo afi, Guid pluginId)
        {
            return LoadFile(stateInfo, afi, pluginId, new LoadFileContext());
        }

        /// <inheritdoc />
        public async Task<LoadResult> LoadFile(IStateInfo stateInfo, IArchiveFileInfo afi, Guid pluginId, LoadFileContext loadFileContext)
        {
            // If stateInfo is no archive state
            if (!(stateInfo.PluginState is IArchiveState archiveState))
                throw new InvalidOperationException("The state represents no archive.");

            // If file is already loaded
            var absoluteFilePath = UPath.Combine(stateInfo.AbsoluteDirectory, stateInfo.FilePath.ToRelative(), afi.FilePath.ToRelative());
            if (IsLoaded(absoluteFilePath))
                return new LoadResult(GetLoadedFile(absoluteFilePath));

            // 1. Create file system action
            var fileSystemAction = new Func<IStreamManager, IFileSystem>(streamManager =>
                  FileSystemFactory.CreateAfiFileSystem(stateInfo, UPath.Root, streamManager));

            // 2. Load file
            // IArchiveFileInfos have stateInfo as their parent, if loaded like this
            var loadResult = await LoadFile(fileSystemAction, afi.FilePath, stateInfo, pluginId, loadFileContext);
            if (!loadResult.IsSuccessful)
                return loadResult;

            // 3. Add archive child to parent
            // ArchiveChildren are only added, if a file is loaded like this
            stateInfo.ArchiveChildren.Add(loadResult.LoadedState);

            return loadResult;
        }

        #endregion

        #region Load FileSystem

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path)
        {
            return LoadFile(fileSystem, path, Guid.Empty, null, new LoadFileContext());
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, IStateInfo parentStateInfo)
        {
            return LoadFile(fileSystem, path, Guid.Empty, parentStateInfo, new LoadFileContext());
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, LoadFileContext loadFileContext)
        {
            return LoadFile(fileSystem, path, Guid.Empty, loadFileContext);
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, IStateInfo parentStateInfo, LoadFileContext loadFileContext)
        {
            return LoadFile(fileSystem, path, Guid.Empty, parentStateInfo, loadFileContext);
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, Guid pluginId)
        {
            return LoadFile(fileSystem, path, pluginId, null, new LoadFileContext());
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, Guid pluginId, IStateInfo parentStateInfo)
        {
            return LoadFile(fileSystem, path, pluginId, parentStateInfo, new LoadFileContext());
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, Guid pluginId, LoadFileContext loadFileContext)
        {
            return LoadFile(fileSystem, path, pluginId, null, loadFileContext);
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, Guid pluginId, IStateInfo parentStateInfo, LoadFileContext loadFileContext)
        {
            // Downside of not having ArchiveChildren is not having the states saved below automatically when opened file is saved

            // If file is loaded
            var absoluteFilePath = UPath.Combine(fileSystem.ConvertPathToInternal(UPath.Root), path.ToRelative());
            if (IsLoaded(absoluteFilePath))
                return Task.FromResult(new LoadResult(GetLoadedFile(absoluteFilePath)));

            // 1. Create file system action
            var fileSystemAction = new Func<IStreamManager, IFileSystem>(fileSystem.Clone);

            // 2. Load file
            // Only if called by a SubPluginManager the parent state is not null
            // Does not add ArchiveChildren to parent state
            return LoadFile(fileSystemAction, path, parentStateInfo, pluginId, loadFileContext);
        }

        #endregion

        #region Load Stream

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(Stream stream, UPath streamName)
        {
            return LoadFile(stream, streamName, Guid.Empty, new LoadFileContext());
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(Stream stream, UPath streamName, LoadFileContext loadFileContext)
        {
            return LoadFile(stream, streamName, Guid.Empty, loadFileContext);
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(Stream stream, UPath streamName, Guid pluginId)
        {
            return LoadFile(stream, streamName, pluginId, new LoadFileContext());
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(Stream stream, UPath streamName, Guid pluginId, LoadFileContext loadFileContext)
        {
            // We don't check for an already loaded file here, since that should never happen

            // 1. Create file system action
            var fileSystemAction = new Func<IStreamManager, IFileSystem>(streamManager =>
                FileSystemFactory.CreateMemoryFileSystem(stream, streamName, streamManager));

            // 2. Load file
            // A stream has no parent, since it should never occur to be loaded from somewhere deeper in the system
            return LoadFile(fileSystemAction, streamName, null, pluginId, loadFileContext);
        }

        #endregion

        private async Task<LoadResult> LoadFile(Func<IStreamManager, IFileSystem> fileSystemAction, UPath path, IStateInfo parentStateInfo, Guid pluginId, LoadFileContext loadFileContext)
        {
            // 1. Create stream manager
            var streamManager = _streamMonitor.CreateStreamManager();

            // 2. Create file system
            var fileSystem = fileSystemAction(streamManager);

            // 3. Find plugin
            IFilePlugin plugin = null;
            if (pluginId != Guid.Empty)
                plugin = _filePluginLoaders.Select(pl => pl.GetPlugin(pluginId)).First();

            var progress = loadFileContext.Progress ?? _progress;
            var isRunning = progress.IsRunning();
            if (!isRunning)
                progress.StartProgress();

            // 4. Load file
            var loadResult = await _fileLoader.LoadAsync(fileSystem, path, new LoadInfo
            {
                ParentStateInfo = parentStateInfo,
                StreamManager = streamManager,
                PluginManager = this,
                Plugin = plugin,
                Progress = progress,
                DialogManager = new InternalDialogManager(_dialogManager, loadFileContext.Options),
                AllowManualSelection = AllowManualSelection
            });

            if (!isRunning)
                progress.FinishProgress();

            if (!loadResult.IsSuccessful)
                return loadResult;

            // 5. Add file to loaded files
            lock (_loadedFilesLock)
            {
                _loadedFiles.Add(loadResult.LoadedState);
            }

            return loadResult;
        }

        #endregion

        #region Save File

        /// <inheritdoc />
        public Task<SaveResult> SaveFile(IStateInfo stateInfo)
        {
            return SaveFile(stateInfo, stateInfo.FilePath);
        }

        /// <inheritdoc />
        public async Task<SaveResult> SaveFile(IStateInfo stateInfo, IFileSystem fileSystem, UPath savePath)
        {
            lock (_loadedFilesLock)
            {
                ContractAssertions.IsElementContained(_loadedFiles, stateInfo, "loadedFiles", nameof(stateInfo));
            }

            var isRunning = _progress.IsRunning();
            if (!isRunning)
                _progress.StartProgress();

            var saveResult = await _fileSaver.SaveAsync(stateInfo, fileSystem, savePath, _progress);

            if (!isRunning)
                _progress.FinishProgress();

            return saveResult;
        }

        /// <inheritdoc />
        public async Task<SaveResult> SaveFile(IStateInfo stateInfo, UPath saveName)
        {
            lock (_loadedFilesLock)
            {
                ContractAssertions.IsElementContained(_loadedFiles, stateInfo, "loadedFiles", nameof(stateInfo));
            }

            var isRunning = _progress.IsRunning();
            if (!isRunning)
                _progress.StartProgress();

            var saveResult = await _fileSaver.SaveAsync(stateInfo, saveName, _progress);

            if (!isRunning)
                _progress.FinishProgress();

            return saveResult;
        }

        #endregion

        #region Close File

        public void Close(IStateInfo stateInfo)
        {
            lock (_loadedFilesLock)
            {
                ContractAssertions.IsElementContained(_loadedFiles, stateInfo, "loadedFiles", nameof(stateInfo));
            }

            // Remove state from its parent
            stateInfo.ParentStateInfo?.ArchiveChildren.Remove(stateInfo);

            CloseInternal(stateInfo);
        }

        public void CloseAll()
        {
            lock (_loadedFilesLock)
            {
                foreach (var state in _loadedFiles)
                    state.Dispose();

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
            {
                indirectChildren = _loadedFiles.Where(x => x.ParentStateInfo == stateInfo).ToArray();
            }
            foreach (var indirectChild in indirectChildren)
                CloseInternal(indirectChild);

            // Close state itself
            stateInfo.Dispose();

            // Remove from the file tracking of this instance
            lock (_loadedFilesLock)
            {
                _loadedFiles.Remove(stateInfo);
            }
        }

        #endregion

        private void FileLoader_OnManualSelection(object sender, ManualSelectionEventArgs e)
        {
            OnManualSelection?.Invoke(sender, e);
        }

        public void Dispose()
        {
            CloseAll();

            _streamMonitor?.Dispose();
        }
    }
}
