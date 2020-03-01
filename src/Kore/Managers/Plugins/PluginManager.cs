using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kontract;
using Kontract.Interfaces;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Loaders;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Progress;
using Kontract.Models;
using Kontract.Models.Archive;
using Kontract.Models.IO;
using Kore.Managers.Plugins.FileManagement;
using Kore.Managers.Plugins.PluginLoader;
using Kore.Models.LoadInfo;

namespace Kore.Managers.Plugins
{
    /// <summary>
    /// The core component of the Kuriimu runtime.
    /// </summary>
    public class PluginManager : IInternalPluginManager
    {
        private readonly IPluginLoader<IFilePlugin>[] _filePluginLoaders;

        private readonly IFileLoader _fileLoader;
        private readonly IFileSaver _fileSaver;

        private readonly IList<IStateInfo> _loadedFiles;

        /// <summary>
        /// Creates a new instance of <see cref="PluginManager"/>.
        /// </summary>
        /// <param name="pluginPaths">The paths to search for plugins.</param>
        public PluginManager(params string[] pluginPaths)
        {
            _filePluginLoaders = new IPluginLoader<IFilePlugin>[] { new CsPluginLoader(pluginPaths) };

            _fileLoader = new FileLoader(_filePluginLoaders);
            _fileSaver = new FileSaver();

            _loadedFiles = new List<IStateInfo>();
        }

        public bool IsLoaded(UPath filePath)
        {
            return _loadedFiles.Any(x => UPath.Combine(x.FileSystem.ConvertPathToInternal(UPath.Root), x.SubPath, x.FilePath) == filePath);
        }

        public IStateInfo GetLoadedFile(UPath filePath)
        {
            return _loadedFiles.FirstOrDefault(x => UPath.Combine(x.FileSystem.ConvertPathToInternal(UPath.Root), x.SubPath, x.FilePath) == filePath);
        }

        /// <inheritdoc />
        public IPluginLoader<IFilePlugin>[] GetFilePluginLoaders()
        {
            return _filePluginLoaders;
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

        /// <summary>
        /// Loads a physical path into the Kuriimu runtime.
        /// </summary>
        /// <param name="file">The path to the path to load.</param>
        /// <param name="progress">The context to report progress.</param>
        /// <returns>The loaded state of the path.</returns>
        public async Task<IStateInfo> LoadFile(string file, IProgressContext progress = null)
        {
            return await LoadFile(file, Guid.Empty, progress);
        }

        /// <summary>
        /// Loads a physical path into the Kuriimu runtime.
        /// </summary>
        /// <param name="file">The path to the path to load.</param>
        /// <param name="pluginId">The Id of the plugin to use for loading.</param>
        /// <param name="progress">The context to report progress.</param>
        /// <returns>The loaded state of the path.</returns>
        public async Task<IStateInfo> LoadFile(string file, Guid pluginId, IProgressContext progress = null)
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

            var loadedFile = await _fileLoader.LoadAsync(loadInfo, this, progress);

            if (loadedFile == null)
                return null;

            _loadedFiles.Add(loadedFile);
            return loadedFile;
        }

        /// <summary>
        /// Loads a virtual path into the Kuriimu runtime.
        /// </summary>
        /// <param name="stateInfo">The loaded path state to load a path from.</param>
        /// <param name="afi">The path to load from that state.</param>
        /// <param name="progress">The context to report progress.</param>
        /// <returns>The loaded state of the path.</returns>
        public async Task<IStateInfo> LoadFile(IStateInfo stateInfo, ArchiveFileInfo afi, IProgressContext progress = null)
        {
            return await LoadFile(stateInfo, afi, Guid.Empty, progress);
        }

        /// <inheritdoc />
        public async Task<IStateInfo> LoadFile(IStateInfo stateInfo, ArchiveFileInfo afi, Guid pluginId, IProgressContext progress = null)
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

            var loadedFile = await _fileLoader.LoadAsync(loadInfo, this, progress);
            _loadedFiles.Add(loadedFile);

            return loadedFile;
        }

        /// <summary>
        /// Loads a path from any file system into the Kuriimu runtime.
        /// </summary>
        /// <param name="fileSystem">The file system to load from.</param>
        /// <param name="path">The path of the file to load.</param>
        /// <param name="progress">The context to report progress.</param>
        /// <returns>The loaded state of the path.</returns>
        public async Task<IStateInfo> LoadFile(IFileSystem fileSystem, UPath path, IProgressContext progress = null)
        {
            return await LoadFile(fileSystem, path, Guid.Empty, progress);
        }

        /// <summary>
        /// Loads a path from any file system into the Kuriimu runtime.
        /// </summary>
        /// <param name="fileSystem">The file system to load from.</param>
        /// <param name="path">The path of the file to load.</param>
        /// <param name="pluginId">The Id of the plugin to use for loading.</param>
        /// <param name="progress">The context to report progress.</param>
        /// <returns>The loaded state of the path.</returns>
        public async Task<IStateInfo> LoadFile(IFileSystem fileSystem, UPath path, Guid pluginId, IProgressContext progress = null)
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

            var loadedFile = await _fileLoader.LoadAsync(loadInfo, this, progress);
            _loadedFiles.Add(loadedFile);

            return loadedFile;
        }

        public Task SaveFile(IStateInfo stateInfo)
        {
            return SaveFile(stateInfo, stateInfo.FilePath);
        }

        public Task SaveFile(IStateInfo stateInfo, UPath saveName)
        {
            ContractAssertions.IsElementContained(_loadedFiles, stateInfo, "loadedFiles", nameof(stateInfo));

            return _fileSaver.SaveAsync(stateInfo, saveName);
        }

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
    }
}
