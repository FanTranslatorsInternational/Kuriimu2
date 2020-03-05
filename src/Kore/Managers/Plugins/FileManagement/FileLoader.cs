using System;
using System.Threading.Tasks;
using Kontract;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Loaders;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Progress;
using Kontract.Interfaces.Providers;
using Kontract.Models.IO;
using Kore.Extensions;
using Kore.Factories;
using Kore.Models;
using Kore.Models.LoadInfo;

namespace Kore.Managers.Plugins.FileManagement
{
    /// <summary>
    /// Loads files in the runtime of Kuriimu.
    /// </summary>
    internal class FileLoader : IFileLoader
    {
        private readonly IProgressContext _progress;
        private readonly IPluginLoader<IFilePlugin>[] _filePluginLoaders;

        /// <summary>
        /// Creates a new instance of <see cref="FileLoader"/>.
        /// </summary>
        /// <param name="filePluginLoaders">The plugin loaders to use.</param>
        public FileLoader(IProgressContext progress, params IPluginLoader<IFilePlugin>[] filePluginLoaders)
        {
            _progress = progress;
            _filePluginLoaders = filePluginLoaders;
        }

        /// <inheritdoc />
        public async Task<IStateInfo> LoadAsync(PhysicalLoadInfo loadInfo, IPluginManager pluginManager, IProgressContext progress = null)
        {
            // 1. Create stream manager
            var streamManager = new StreamManager();

            // 2. Create file system
            var directory = loadInfo.FilePath.GetDirectory();
            var fileSystem = FileSystemFactory.CreatePhysicalFileSystem(directory, streamManager);

            // 3. Load the file
            var fileName = loadInfo.FilePath.GetName();
            return await InternalLoadAsync(fileSystem, fileName, streamManager, pluginManager, progress, loadInfo.Plugin);
        }

        /// <inheritdoc />
        public async Task<IStateInfo> LoadAsync(VirtualLoadInfo loadInfo, IPluginManager pluginManager, IProgressContext progress = null)
        {
            // 1. Create stream manager
            var streamManager = new StreamManager();

            // 2. Create file system
            var directory = loadInfo.Afi.FilePath.GetDirectory();
            var fileSystem = FileSystemFactory.CreateAfiFileSystem(loadInfo.ArchiveState, directory, streamManager);

            // 3. Load the file
            var fileName = loadInfo.Afi.FilePath.GetName();
            var loadedFile = await InternalLoadAsync(fileSystem, fileName, streamManager, pluginManager, progress, loadInfo.Plugin);

            // 4. Set children and parent accordingly
            loadInfo.ParentStateInfo.ArchiveChildren.Add(loadedFile);
            loadedFile.ParentStateInfo = loadInfo.ParentStateInfo;
            loadedFile.SubPath = directory;

            return loadedFile;
        }

        /// <inheritdoc />
        public async Task<IStateInfo> LoadAsync(PluginLoadInfo loadInfo, IPluginManager pluginManager, IProgressContext progress = null)
        {
            // 1. Create stream manager
            var streamManager = new StreamManager();

            // 2. Create file system
            var directory = loadInfo.FilePath.GetDirectory();
            var fileSystem = FileSystemFactory.CloneFileSystem(loadInfo.FileSystem, directory, streamManager);

            // 3. Load the file
            var fileName = loadInfo.FilePath.GetName();
            var loadedFile = await InternalLoadAsync(fileSystem, fileName, streamManager, pluginManager, progress, loadInfo.Plugin, false);

            // 4. Set parent objects accordingly
            loadedFile.ParentStateInfo = loadedFile;
            loadedFile.SubPath = directory;

            return loadedFile;
        }

        /// <summary>
        /// Loads a specified file from a specified file system.
        /// </summary>
        /// <param name="fileSystem">The file system to retrieve the file from.</param>
        /// <param name="filePath">The path to the file to load.</param>
        /// <param name="streamManager">The stream manager for this file.</param>
        /// <param name="pluginManager">The manager for plugins.</param>
        /// <param name="plugin">The pre specified plugin.</param>
        /// <param name="identifyPluginManually">Defines if the plugin should be identified by a manual selection.</param>
        /// <param name="progress">The progress context for the Kuriimu runtime.</param>
        /// <returns>The loaded file.</returns>
        private async Task<IStateInfo> InternalLoadAsync(
            IFileSystem fileSystem,
            UPath filePath,
            IStreamManager streamManager,
            IPluginManager pluginManager,
            IProgressContext progress = null,
            IFilePlugin plugin = null,
            bool identifyPluginManually = true)
        {
            ContractAssertions.IsNotNull(fileSystem, nameof(fileSystem));
            ContractAssertions.IsNotNull(filePath, nameof(filePath));
            ContractAssertions.IsNotNull(streamManager, nameof(streamManager));

            // 1. Create temporary Stream provider
            var temporaryStreamProvider = streamManager.CreateTemporaryStreamProvider();

            // 2. Identify the plugin to use
            if (plugin == null)
            {
                plugin = await IdentifyPluginAsync(fileSystem, filePath, streamManager, identifyPluginManually);
                if (plugin == null)
                {
                    streamManager.ReleaseAll();

                    // TODO: Handle errors
                    return null;
                }
            }

            // 3. Create state from identified plugin
            var state = plugin.CreatePluginState(pluginManager);

            // 4. Load data from state
            if (!await TryLoadStateAsync(state, fileSystem, filePath, temporaryStreamProvider))
            {
                streamManager.ReleaseAll();

                // TODO: Handle error
                return null;
            }

            // 5. Create new state info
            return new StateInfo(state, fileSystem, filePath, streamManager);
        }

        /// <summary>
        /// Identify the plugin to load the file.
        /// </summary>
        /// <param name="fileSystem">The file system to retrieve the file from.</param>
        /// <param name="filePath">The path of the file to identify.</param>
        /// <param name="streamManager">The stream manager.</param>
        /// <param name="identifyPluginManually">Defines if the plugin should be identified by a manual selection.</param>
        /// <returns>The identified <see cref="IFilePlugin"/>.</returns>
        private async Task<IFilePlugin> IdentifyPluginAsync(IFileSystem fileSystem, UPath filePath, IStreamManager streamManager, bool identifyPluginManually)
        {
            // 1. Get all plugins that implement IIdentifyFile
            var identifiablePlugins = _filePluginLoaders.GetIdentifiableFilePlugins();

            foreach (var identifiablePlugin in identifiablePlugins)
            {
                // 2. Identify the file with the next plugin
                var identifyResult = await TryIdentifyFileAsync(identifiablePlugin, fileSystem, filePath, streamManager);

                // 3. Return first plugin that could identify
                if (identifyResult)
                    return identifiablePlugin as IFilePlugin;
            }

            // 4. Return null, if no plugin could identify and manual selection is disabled
            if (!identifyPluginManually)
                return null;

            // 5. If no plugin could identify the file, get manual feedback on all plugins that don't implement IIdentifyFiles
            return GetManualSelection();
        }

        /// <summary>
        /// Identify a file with a single plugin.
        /// </summary>
        /// <param name="identifyFile">The plugin to identify with.</param>
        /// <param name="fileSystem">The file system to retrieve the file from.</param>
        /// <param name="filePath">The path of the file to identify.</param>
        /// <param name="streamManager">The stream manager.</param>
        /// <returns>If hte identification was successful.</returns>
        private async Task<bool> TryIdentifyFileAsync(IIdentifyFiles identifyFile, IFileSystem fileSystem, UPath filePath, IStreamManager streamManager)
        {
            // 1. Identify plugin
            var identifyResult = await identifyFile.IdentifyAsync(fileSystem, filePath, streamManager.CreateTemporaryStreamProvider());

            // 2. Close all streams opened by the identifying method
            streamManager.ReleaseAll();

            return identifyResult;
        }

        /// <summary>
        /// Select a plugin manually.
        /// </summary>
        /// <returns>The manually selected plugin.</returns>
        private IFilePlugin GetManualSelection()
        {
            // 1. Get all plugins that don't implement IIdentifyFile
            var nonIdentifiablePlugins = _filePluginLoaders.GetNonIdentifiableFilePlugins();

            // TODO: 2. Request selection of non identifiable plugins from external sources

            // TODO: 3. Return selection

            // TODO: Remove stub return from manual selection
            return null;
        }

        /// <summary>
        /// Try to load the state for the plugin.
        /// </summary>
        /// <param name="pluginState">The plugin state to load.</param>
        /// <param name="fileSystem">The file system to retrieve further files from.</param>
        /// <param name="filePath">The path of the identified file.</param>
        /// <param name="temporaryStreamProvider">The stream provider for temporary files.</param>
        /// <returns>If the loading was successful.</returns>
        private async Task<bool> TryLoadStateAsync(IPluginState pluginState, IFileSystem fileSystem, UPath filePath, ITemporaryStreamProvider temporaryStreamProvider)
        {
            // 1. Check if state implements ILoadFile
            if (!(pluginState is ILoadFiles loadableState))
            {
                return false;
            }

            // 2. Try loading the state
            try
            {
                await Task.Factory.StartNew(() => loadableState.Load(fileSystem, filePath, temporaryStreamProvider, _progress));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
