using System;
using System.Linq;
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
using Kontract.Models;
using Kontract.Models.IO;
using Kore.Extensions;
using Kore.Factories;
using Kore.Models;
using Kore.Models.LoadInfo;
using Kore.Models.UnsupportedPlugin;

namespace Kore.Managers.Plugins.FileManagement
{
    /// <summary>
    /// Loads files in the runtime of Kuriimu.
    /// </summary>
    internal class FileLoader : IFileLoader
    {
        private readonly IProgressContext _progress;
        private readonly IPluginLoader<IFilePlugin>[] _filePluginLoaders;

        public event EventHandler<ManualSelectionEventArgs> OnManualSelection;

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
        public async Task<LoadResult> LoadAsync(PhysicalLoadInfo loadInfo, IPluginManager pluginManager,
            bool loadPluginManually, IProgressContext progress = null)
        {
            // 1. Create stream manager
            var streamManager = new StreamManager();

            // 2. Create file system
            var directory = loadInfo.FilePath.GetDirectory();
            var fileSystem = FileSystemFactory.CreatePhysicalFileSystem(directory, streamManager);

            // 3. Load the file
            var fileName = loadInfo.FilePath.GetName();
            return await InternalLoadAsync(fileSystem, fileName, streamManager, pluginManager, progress, loadInfo.Plugin, loadPluginManually);
        }

        /// <inheritdoc />
        public async Task<LoadResult> LoadAsync(VirtualLoadInfo loadInfo, IPluginManager pluginManager,
            bool loadPluginManually, IProgressContext progress = null)
        {
            // 1. Create stream manager
            var streamManager = new StreamManager();

            // 2. Create file system
            var fileSystem = FileSystemFactory.CreateAfiFileSystem(loadInfo.ArchiveState, UPath.Empty, streamManager);

            // 3. Load the file
            var loadResult = await InternalLoadAsync(fileSystem, loadInfo.Afi.FilePath, streamManager, pluginManager, progress, loadInfo.Plugin, loadPluginManually);
            if (!loadResult.IsSuccessful)
                return loadResult;

            // 4. Set children and parent accordingly
            loadInfo.ParentStateInfo.ArchiveChildren.Add(loadResult.LoadedState);
            loadResult.LoadedState.ParentStateInfo = loadInfo.ParentStateInfo;

            return loadResult;
        }

        /// <inheritdoc />
        public async Task<LoadResult> LoadAsync(PluginLoadInfo loadInfo, IPluginManager pluginManager,
            bool loadPluginManually, IProgressContext progress = null)
        {
            // 1. Create stream manager
            var streamManager = new StreamManager();

            // 2. Create file system
            var fileSystem = FileSystemFactory.CloneFileSystem(loadInfo.FileSystem, UPath.Empty, streamManager);

            // 3. Load the file
            var loadResult = await InternalLoadAsync(fileSystem, loadInfo.FilePath, streamManager, pluginManager, progress, loadInfo.Plugin, loadPluginManually);
            if (!loadResult.IsSuccessful)
                return loadResult;

            // TODO: 4. Set parent objects accordingly
            loadResult.LoadedState.ParentStateInfo = loadResult.LoadedState;

            return loadResult;
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
        private async Task<LoadResult> InternalLoadAsync(
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
                plugin = await IdentifyPluginAsync(fileSystem, filePath, streamManager, identifyPluginManually) ??
                         new HexPlugin();
            }

            // 3. Create state from identified plugin
            var state = plugin.CreatePluginState(pluginManager);

            // 4. Load data from state
            var loadStateResult = await TryLoadStateAsync(state, fileSystem, filePath, temporaryStreamProvider);
            if (!loadStateResult.IsSuccessful)
            {
                streamManager.ReleaseAll();
                return loadStateResult;
            }

            // 5. Create new state info
            var stateInfo = new StateInfo(state, fileSystem, filePath, streamManager);
            return new LoadResult(stateInfo);
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
            var nonIdentifiablePlugins = _filePluginLoaders.GetNonIdentifiableFilePlugins().ToArray();

            // 2. Request manual selection by the user
            var selectionArgs = new ManualSelectionEventArgs(nonIdentifiablePlugins);
            OnManualSelection?.Invoke(this, selectionArgs);

            return selectionArgs.Result;
        }

        /// <summary>
        /// Try to load the state for the plugin.
        /// </summary>
        /// <param name="pluginState">The plugin state to load.</param>
        /// <param name="fileSystem">The file system to retrieve further files from.</param>
        /// <param name="filePath">The path of the identified file.</param>
        /// <param name="temporaryStreamProvider">The stream provider for temporary files.</param>
        /// <returns>If the loading was successful.</returns>
        private async Task<LoadResult> TryLoadStateAsync(IPluginState pluginState, IFileSystem fileSystem, UPath filePath, ITemporaryStreamProvider temporaryStreamProvider)
        {
            // 1. Check if state implements ILoadFile
            if (!(pluginState is ILoadFiles loadableState))
                return new LoadResult(false, "The state is not loadable.");

            // 2. Try loading the state
            try
            {
                await loadableState.Load(fileSystem, filePath, temporaryStreamProvider, _progress);
            }
            catch (Exception ex)
            {
                return new LoadResult(ex);
            }

            return new LoadResult(true);
        }
    }
}
