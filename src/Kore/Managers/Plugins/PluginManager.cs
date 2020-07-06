using System;
using System.Collections.Generic;
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
using Kontract.Interfaces.Providers;
using Kontract.Models;
using Kontract.Models.Archive;
using Kontract.Models.IO;
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

        private readonly IList<IStateInfo> _loadedFiles;

        /// <inheritdoc />
        public event EventHandler<ManualSelectionEventArgs> OnManualSelection;

        /// <inheritdoc />
        public bool AllowManualSelection { get; set; } = true;

        public IFileSystemProvider FileSystemProvider => throw new NotSupportedException();

        /// <inheritdoc />
        public IReadOnlyList<PluginLoadError> LoadErrors { get; }

        /// <summary>
        /// Creates a new instance of <see cref="PluginManager"/>.
        /// </summary>
        /// <param name="pluginPaths">The paths to search for plugins.</param>
        public PluginManager(params string[] pluginPaths) :
            this(new ConcurrentProgress(new NullProgressOutput()), new DefaultDialogManager(), pluginPaths)
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
            this(new ConcurrentProgress(new NullProgressOutput()), dialogManager, pluginPaths)
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

            _filePluginLoaders = new IPluginLoader<IFilePlugin>[] { new CsFilePluginLoader(pluginPaths) };
            _gameAdapterLoaders = new IPluginLoader<IGameAdapter>[] { new CsGamePluginLoader(pluginPaths) };
            _progress = progress;

            LoadErrors = _filePluginLoaders.SelectMany(pl => pl.LoadErrors ?? Array.Empty<PluginLoadError>())
                .Concat(_gameAdapterLoaders.SelectMany(pl => pl.LoadErrors ?? Array.Empty<PluginLoadError>()))
                .DistinctBy(e => e.AssemblyPath)
                .ToList();

            _fileLoader = new FileLoader(dialogManager, _filePluginLoaders);
            _fileSaver = new FileSaver(dialogManager);

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

        /// <inheritdoc />
        public bool IsLoaded(UPath filePath)
        {
            return _loadedFiles.Any(x =>
                x.AbsoluteDirectory / x.FilePath.ToRelative() == filePath);
        }

        /// <inheritdoc />
        public IStateInfo GetLoadedFile(UPath filePath)
        {
            return _loadedFiles.FirstOrDefault(x => x.AbsoluteDirectory / x.FilePath.ToRelative() == filePath);
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

        #region Load File

        /// <inheritdoc />
        public async Task<LoadResult> LoadFile(string file, IProgressContext progress = null)
        {
            return await LoadFile(file, Guid.Empty, progress ?? _progress);
        }

        /// <inheritdoc />
        public async Task<LoadResult> LoadFile(string file, Guid pluginId, IProgressContext progress = null)
        {
            PhysicalLoadInfo loadInfo;
            if (pluginId != Guid.Empty && _filePluginLoaders.Any(pl => pl.Exists(pluginId)))
            {
                var plugin = _filePluginLoaders.Select(pl => pl.GetPlugin(pluginId)).First();

                loadInfo = new PhysicalLoadInfo(file, plugin);
            }
            else
            {
                loadInfo = new PhysicalLoadInfo(file);
            }

            var loadResult = await _fileLoader.LoadAsync(loadInfo, this, AllowManualSelection, progress ?? _progress);

            if (!loadResult.IsSuccessful)
                return loadResult;

            _loadedFiles.Add(loadResult.LoadedState);
            return loadResult;
        }

        /// <inheritdoc />
        public async Task<LoadResult> LoadFile(IStateInfo stateInfo, ArchiveFileInfo afi, IProgressContext progress = null)
        {
            return await LoadFile(stateInfo, afi, Guid.Empty, progress ?? _progress);
        }

        /// <inheritdoc />
        public async Task<LoadResult> LoadFile(IStateInfo stateInfo, ArchiveFileInfo afi, Guid pluginId, IProgressContext progress = null)
        {
            if (!(stateInfo.State is IArchiveState archiveState))
                throw new InvalidOperationException("The state represents no archive.");

            VirtualLoadInfo loadInfo;
            if (pluginId != Guid.Empty && _filePluginLoaders.Any(pl => pl.Exists(pluginId)))
            {
                var plugin = _filePluginLoaders.Select(pl => pl.GetPlugin(pluginId)).First();

                loadInfo = new VirtualLoadInfo(stateInfo, archiveState, afi, plugin);
            }
            else
            {
                loadInfo = new VirtualLoadInfo(stateInfo, archiveState, afi);
            }

            var loadResult = await _fileLoader.LoadAsync(loadInfo, this, AllowManualSelection, progress ?? _progress);
            if (!loadResult.IsSuccessful)
                return loadResult;

            _loadedFiles.Add(loadResult.LoadedState);
            return loadResult;
        }

        /// <inheritdoc />
        public async Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, IProgressContext progress = null)
        {
            return await LoadFile(fileSystem, path, Guid.Empty, progress ?? _progress);
        }

        /// <inheritdoc />
        public async Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, Guid pluginId, IProgressContext progress = null)
        {
            PluginLoadInfo loadInfo;
            if (pluginId != Guid.Empty && _filePluginLoaders.Any(pl => pl.Exists(pluginId)))
            {
                var plugin = _filePluginLoaders.Select(pl => pl.GetPlugin(pluginId)).First();

                loadInfo = new PluginLoadInfo(fileSystem, path, plugin);
            }
            else
            {
                loadInfo = new PluginLoadInfo(fileSystem, path);
            }

            var loadResult = await _fileLoader.LoadAsync(loadInfo, this, AllowManualSelection, progress ?? _progress);
            if (!loadResult.IsSuccessful)
                return loadResult;

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

        /// <inheritdoc />
        public Task<SaveResult> SaveFile(IStateInfo stateInfo, IFileSystem fileSystem, UPath savePath)
        {
            return _fileSaver.SaveAsync(stateInfo, fileSystem, savePath, _progress);
        }

        /// <inheritdoc />
        public Task<SaveResult> SaveFile(IStateInfo stateInfo, UPath saveName)
        {
            ContractAssertions.IsElementContained(_loadedFiles, stateInfo, "loadedFiles", nameof(stateInfo));

            return _fileSaver.SaveAsync(stateInfo, saveName, _progress);
        }

        #endregion

        public void Close(IStateInfo stateInfo)
        {
            ContractAssertions.IsElementContained(_loadedFiles, stateInfo, "loadedFiles", nameof(stateInfo));

            // Remove state from its parent
            stateInfo.ParentStateInfo?.ArchiveChildren.Remove(stateInfo);

            CloseInternal(stateInfo);
        }

        public void CloseAll()
        {
            foreach (var state in _loadedFiles)
                state.Dispose();

            _loadedFiles.Clear();
        }

        private void CloseInternal(IStateInfo stateInfo)
        {
            ContractAssertions.IsNotNull(stateInfo, nameof(stateInfo));

            // Close children of this state first
            foreach (var child in stateInfo.ArchiveChildren)
                CloseInternal(child);

            // Close state itself
            stateInfo.Dispose();

            // Remove from the file tracking of this instance
            _loadedFiles.Remove(stateInfo);
        }

        private void FileLoader_OnManualSelection(object sender, ManualSelectionEventArgs e)
        {
            OnManualSelection?.Invoke(sender, e);
        }
    }
}
