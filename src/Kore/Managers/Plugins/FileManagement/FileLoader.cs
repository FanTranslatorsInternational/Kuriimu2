using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kontract;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Loaders;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;
using Kontract.Models.Context;
using Kontract.Models.IO;
using Kore.Extensions;
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
        private readonly IPluginLoader<IFilePlugin>[] _filePluginLoaders;

        public event EventHandler<ManualSelectionEventArgs> OnManualSelection;

        /// <summary>
        /// Creates a new instance of <see cref="FileLoader"/>.
        /// </summary>
        /// <param name="filePluginLoaders">The plugin loaders to use.</param>
        public FileLoader(params IPluginLoader<IFilePlugin>[] filePluginLoaders)
        {
            _filePluginLoaders = filePluginLoaders;
        }

        /// <inheritdoc />
        public async Task<LoadResult> LoadAsync(IFileSystem fileSystem, UPath filePath, LoadInfo loadInfo)
        {
            ContractAssertions.IsNotNull(fileSystem, nameof(fileSystem));
            ContractAssertions.IsNotNull(filePath, nameof(filePath));

            // 1. Create temporary Stream provider
            var temporaryStreamProvider = loadInfo.StreamManager.CreateTemporaryStreamProvider();

            // 2. Identify the plugin to use
            var plugin = loadInfo.Plugin ??
                         await IdentifyPluginAsync(fileSystem, filePath, loadInfo) ??
                         new HexPlugin();

            // 3. Create state from identified plugin
            var subPluginManager = new SubPluginManager(loadInfo.PluginManager);
            var createResult = TryCreateState(plugin, subPluginManager, out var state);
            if (!createResult.IsSuccessful)
                return createResult;

            // 4. Create new state info
            var stateInfo = new StateInfo(plugin, state, loadInfo.ParentStateInfo, fileSystem, filePath, loadInfo.StreamManager, subPluginManager);
            subPluginManager.RegisterStateInfo(stateInfo);

            // 5. Load data from state
            var loadContext = new LoadContext(temporaryStreamProvider, loadInfo.Progress, loadInfo.DialogManager);
            var loadStateResult = await TryLoadStateAsync(state, fileSystem, filePath, loadContext);
            if (!loadStateResult.IsSuccessful)
            {
                loadInfo.StreamManager.ReleaseAll();
                stateInfo.Dispose();

                return loadStateResult;
            }

            stateInfo.SetDialogOptions(loadInfo.DialogManager.DialogOptions);
            return new LoadResult(stateInfo);
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
            // 1. Get all plugins that implement IIdentifyFile
            var identifiablePlugins = _filePluginLoaders.GetIdentifiableFilePlugins();

            // 2. Identify the file with identifiable plugins
            var matchedPlugins = new List<IFilePlugin>();
            foreach (var identifiablePlugin in identifiablePlugins)
            {
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
            if (matchedPlugins.Count == 1)
                return matchedPlugins.First();

            if (matchedPlugins.Count > 1)
                return GetManualSelection(matchedPlugins);

            // 5. If no plugin could identify the file, get manual feedback on all plugins that don't implement IIdentifyFiles
            var nonIdentifiablePlugins = _filePluginLoaders.GetNonIdentifiableFilePlugins().ToArray();
            return loadInfo.AllowManualSelection ? GetManualSelection(nonIdentifiablePlugins) : null;
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
        private IFilePlugin GetManualSelection(IReadOnlyList<IFilePlugin> pluginList)
        {
            // 1. Request manual selection by the user
            var selectionArgs = new ManualSelectionEventArgs(pluginList);
            OnManualSelection?.Invoke(this, selectionArgs);

            return selectionArgs.Result;
        }

        /// <summary>
        /// Try to create a new plugin state.
        /// </summary>
        /// <param name="plugin">The plugin from which to create a new state.</param>
        /// <param name="pluginManager">The plugin manager to pass to the state creation.</param>
        /// <param name="pluginState">The created state.</param>
        /// <returns>If the creation was successful.</returns>
        private LoadResult TryCreateState(IFilePlugin plugin, IPluginManager pluginManager, out IPluginState pluginState)
        {
            pluginState = null;

            try
            {
                pluginState = plugin.CreatePluginState(pluginManager);
            }
            catch (Exception e)
            {
                return new LoadResult(e);
            }

            return new LoadResult(true);
        }

        /// <summary>
        /// Try to load the state for the plugin.
        /// </summary>
        /// <param name="pluginState">The plugin state to load.</param>
        /// <param name="fileSystem">The file system to retrieve further files from.</param>
        /// <param name="filePath">The path of the identified file.</param>
        /// <param name="loadContext">The load context.</param>
        /// <returns>If the loading was successful.</returns>
        private async Task<LoadResult> TryLoadStateAsync(IPluginState pluginState, IFileSystem fileSystem, UPath filePath,
            LoadContext loadContext)
        {
            // 1. Check if state implements ILoadFile
            if (!(pluginState is ILoadFiles loadableState))
                return new LoadResult(false, "The state is not loadable.");

            // 2. Try loading the state
            try
            {
                await Task.Run(async () => await loadableState.Load(fileSystem, filePath, loadContext));
            }
            catch (Exception ex)
            {
                return new LoadResult(ex);
            }

            return new LoadResult(true);
        }
    }
}
