using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kontract;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers.Files;
using Kontract.Interfaces.Managers.Streams;
using Kontract.Interfaces.Plugins.Entry;
using Kontract.Interfaces.Plugins.Loaders;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.FileSystem;
using Kontract.Models.Managers.Files;
using Kontract.Models.Plugins.Entry;
using Kontract.Models.Plugins.State;
using Kore.Extensions;
using Kore.Models.Managers.Files;
using Kore.Models.Managers.Files.Support;

namespace Kore.Implementation.Managers.Files.Support
{
    /// <summary>
    /// Loads files in the runtime of Kuriimu.
    /// </summary>
    internal class FileLoader
    {
        private readonly IPluginLoader<IFilePlugin>[] _filePluginLoaders;

        public event EventHandler<KoreFileManager.ManualSelectionEventArgs> OnManualSelection;

        /// <summary>
        /// Creates a new instance of <see cref="FileLoader"/>.
        /// </summary>
        /// <param name="filePluginLoaders">The plugin loaders to use.</param>
        public FileLoader(params IPluginLoader<IFilePlugin>[] filePluginLoaders)
        {
            _filePluginLoaders = filePluginLoaders;
        }

        /// <summary>
        /// Loads a file from a file system into the runtime.
        /// </summary>
        /// <param name="fileSystem">The <see cref="IFileSystem"/> to load the file from.</param>
        /// <param name="filePath">The <see cref="UPath"/> to the file.</param>
        /// <param name="loadInfo">Additional information pertaining to loading a file.</param>
        /// <returns>The result of the load operation.</returns>
        public async Task<KoreLoadResult> LoadAsync(IFileSystem fileSystem, UPath filePath, LoadInfo loadInfo)
        {
            ContractAssertions.IsNotNull(fileSystem, nameof(fileSystem));
            ContractAssertions.IsNotNull(filePath, nameof(filePath));

            // 1. Create temporary Stream provider
            var temporaryStreamProvider = loadInfo.StreamManager.CreateTemporaryStreamProvider();

            // 2. Identify the plugin to use
            var plugin = loadInfo.Plugin ?? await IdentifyPluginAsync(fileSystem, filePath, loadInfo);
            if (plugin == null)
                return KoreLoadResult.Cancel(LoadErrorReason.NoPlugin);

            loadInfo.Logger.Information("PluginId: {0}", plugin.PluginId);

            // 3. Create state from identified plugin
            var subPluginManager = new ScopedFileManager(loadInfo.KoreFileManager);
            var createResult = TryCreateState(plugin, subPluginManager, out var state);
            if (createResult != null)
                return createResult;

            // 4. Create new state info
            var stateInfo = new FileState(plugin, state, loadInfo.ParentFileState, fileSystem, filePath, loadInfo.StreamManager, subPluginManager);
            subPluginManager.RegisterStateInfo(stateInfo);

            // 5. Load data from state
            var loadContext = new LoadContext(temporaryStreamProvider, loadInfo.Progress, loadInfo.DialogManager);
            var loadStateResult = await TryLoadStateAsync(state, fileSystem, filePath, loadContext);
            if (loadStateResult != null)
            {
                loadInfo.StreamManager.ReleaseAll();
                stateInfo.Dispose();

                return loadStateResult;
            }

            stateInfo.SetDialogOptions(loadInfo.DialogManager.DialogOptions);

            return new KoreLoadResult(stateInfo);
        }

        /// <summary>
        /// Identify the plugin to load the file.
        /// </summary>
        /// <param name="fileSystem">The file system to retrieve the file from.</param>
        /// <param name="filePath">The path of the file to identify.</param>
        /// <param name="loadInfo">The context for the load operation.</param>
        /// <returns>The identified <see cref="IFilePlugin"/>.</returns>
        private async Task<IFilePlugin> IdentifyPluginAsync(IFileSystem fileSystem, UPath filePath, LoadInfo loadInfo)
        {
            // 1. Get all plugins that support identification
            var identifiablePlugins = _filePluginLoaders.GetIdentifiableFilePlugins();

            // 2. Identify the file with identifiable plugins
            var matchedPlugins = new List<IFilePlugin>();
            foreach (var identifiablePlugin in identifiablePlugins)
            {
                //TODO this cast smells, all IIdentifyFiles should be IFilePlugins (right?)
                var filePlugin = identifiablePlugin as IFilePlugin;

                try
                {
                    var identifyResult = await Task.Run(async () => await TryIdentifyFileAsync(identifiablePlugin, fileSystem, filePath, loadInfo.StreamManager));
                    if (identifyResult)
                        matchedPlugins.Add(filePlugin);
                }
                catch (Exception e)
                {
                    // Log exceptions and carry on
                    loadInfo.Logger?.Fatal(e, "Tried to identify file '{0}' with plugin '{1}'.", filePath.FullName, filePlugin?.PluginId);
                }
            }

            // 3. Return only matched plugin or manually select one of the matched plugins
            var allPlugins = _filePluginLoaders.GetAllFilePlugins().ToArray();

            if (matchedPlugins.Count == 1)
                return matchedPlugins.First();

            if (matchedPlugins.Count > 1)
                return GetManualSelection(allPlugins, matchedPlugins, KoreFileManager.SelectionStatus.MultipleMatches);

            // 5. If no plugin could identify the file, get manual feedback on all plugins that don't implement IIdentifyFiles
            return loadInfo.AllowManualSelection ? GetManualSelection(allPlugins, allPlugins.Where(x => !x.CanIdentifyFiles), KoreFileManager.SelectionStatus.NonIdentifiable) : null;
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
            var identifyContext = new IdentifyContext(streamManager.CreateTemporaryStreamProvider());
            var identifyResult = await identifyFile.IdentifyAsync(fileSystem, filePath, identifyContext);

            // 2. Close all streams opened by the identifying method
            streamManager.ReleaseAll();

            return identifyResult;
        }

        /// <summary>
        /// Select a plugin manually.
        /// </summary>
        /// <returns>The manually selected plugin.</returns>
        private IFilePlugin GetManualSelection(IEnumerable<IFilePlugin> allFilePlugins, IEnumerable<IFilePlugin> filteredFilePlugins, KoreFileManager.SelectionStatus status)
        {
            // 1. Request manual selection by the user
            var selectionArgs = new KoreFileManager.ManualSelectionEventArgs(allFilePlugins, filteredFilePlugins, status);
            OnManualSelection?.Invoke(this, selectionArgs);

            return selectionArgs.Result;
        }

        /// <summary>
        /// Try to create a new plugin state.
        /// </summary>
        /// <param name="plugin">The plugin from which to create a new state.</param>
        /// <param name="fileManager">The plugin manager to pass to the state creation.</param>
        /// <param name="pluginState">The created state.</param>
        /// <returns>If the creation was successful.</returns>
        private KoreLoadResult TryCreateState(IFilePlugin plugin, IFileManager fileManager, out IPluginState pluginState)
        {
            pluginState = null;

            try
            {
                pluginState = plugin.CreatePluginState(fileManager);
            }
            catch (Exception e)
            {
                return new KoreLoadResult(LoadErrorReason.StateCreateError, e);
            }

            return null;
        }

        /// <summary>
        /// Try to load the state for the plugin.
        /// </summary>
        /// <param name="pluginState">The plugin state to load.</param>
        /// <param name="fileSystem">The file system to retrieve further files from.</param>
        /// <param name="filePath">The path of the identified file.</param>
        /// <param name="loadContext">The load context.</param>
        /// <returns>If the loading was successful.</returns>
        private async Task<KoreLoadResult> TryLoadStateAsync(IPluginState pluginState, IFileSystem fileSystem, UPath filePath,
            LoadContext loadContext)
        {
            // 1. Check if state supports loading
            if (!pluginState.CanLoad)
                return new KoreLoadResult(LoadErrorReason.StateNoLoad);

            // 2. Try loading the state
            try
            {
                await Task.Run(async () => await pluginState.AttemptLoad(fileSystem, filePath, loadContext));
            }
            catch (Exception e)
            {
                return new KoreLoadResult(LoadErrorReason.StateLoadError, e);
            }

            return null;
        }
    }
}
