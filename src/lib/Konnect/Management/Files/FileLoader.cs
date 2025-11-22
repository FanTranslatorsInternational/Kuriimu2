using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Management.Files;
using Konnect.Contract.DataClasses.Management.Files.Events;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Management.Files;
using Konnect.Contract.Exceptions.Management.Files;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Management.Plugin;
using Konnect.Contract.Management.Streams;
using Konnect.Contract.Plugin.File;

namespace Konnect.Management.Files;

/// <summary>
/// Loads files in the runtime of Kuriimu.
/// </summary>
internal class FileLoader : IFileLoader
{
    private readonly IPluginManager _pluginManager;

    public event ManualSelectionDelegate? OnManualSelection;

    /// <summary>
    /// Creates a new instance of <see cref="FileLoader"/>.
    /// </summary>
    /// <param name="pluginManager">The plugin manager to use.</param>
    public FileLoader(IPluginManager pluginManager)
    {
        _pluginManager = pluginManager;
    }

    /// <inheritdoc />
    public async Task<LoadResult> LoadAsync(IFileSystem fileSystem, UPath filePath, LoadFileOptions loadInfo)
    {
        // 1. Create temporary Stream provider
        var temporaryStreamProvider = loadInfo.StreamManager.CreateTemporaryStreamProvider();

        // 2. Identify the plugin to use
        var plugin = loadInfo.Plugin ?? await IdentifyPluginAsync(fileSystem, filePath, loadInfo);
        if (plugin == null)
            return new LoadResult
            {
                Status = LoadStatus.Errored,
                Reason = LoadErrorReason.NoPlugin
            };

        // 3. Create state from identified plugin
        var subPluginManager = new ScopedFileManager(loadInfo.FileManager);
        var createResult = TryCreateState(plugin, subPluginManager, loadInfo, out var state);
        if (createResult.Status != LoadStatus.Successful)
            return createResult;

        // 4. Create new state info
        var stateInfo = new FileState(plugin, state, loadInfo.ParentFileState, fileSystem, filePath, loadInfo.StreamManager, subPluginManager);
        subPluginManager.RegisterStateInfo(stateInfo);

        // 5. Load data from state
        var loadContext = new LoadContext
        {
            DialogManager = loadInfo.DialogManager,
            TemporaryStreamManager = temporaryStreamProvider,
            ProgressContext = loadInfo.Progress
        };
        var loadStateResult = await TryLoadStateAsync(state, fileSystem, filePath, loadContext, loadInfo, plugin);
        if (loadStateResult.Status != LoadStatus.Successful)
        {
            loadInfo.StreamManager.ReleaseAll();
            stateInfo.Dispose();

            return loadStateResult;
        }

        if (loadInfo.DialogManager != null)
            stateInfo.SetDialogOptions(loadInfo.DialogManager.DialogOptions);

        return new LoadResult
        {
            Status = LoadStatus.Successful,
            LoadedFileState = stateInfo,
            Reason = LoadErrorReason.None
        };
    }

    /// <summary>
    /// Identify the plugin to load the file.
    /// </summary>
    /// <param name="fileSystem">The file system to retrieve the file from.</param>
    /// <param name="filePath">The path of the file to identify.</param>
    /// <param name="loadInfo">The context for the load operation.</param>
    /// <returns>The identified <see cref="IFilePlugin"/>.</returns>
    private async Task<IFilePlugin?> IdentifyPluginAsync(IFileSystem fileSystem, UPath filePath, LoadFileOptions loadInfo)
    {
        // 1. Get all plugins that support identification
        var identifiablePlugins = _pluginManager.GetPlugins<IFilePlugin>().Where(p => p.CanIdentifyFiles);

        // 2. Identify the file with identifiable plugins
        var matchedPlugins = new List<IFilePlugin>();
        foreach (IFilePlugin identifiablePlugin in identifiablePlugins)
        {
            try
            {
                var identifyResult = await Task.Run(async () => await TryIdentifyFileAsync(identifiablePlugin, fileSystem, filePath, loadInfo.StreamManager));
                if (identifyResult)
                    matchedPlugins.Add(identifiablePlugin);
            }
            catch (Exception e)
            {
                // Log exceptions and carry on
                loadInfo.Logger?.Fatal(e, "Tried to identify file '{0}' with plugin '{1}'.", filePath.FullName, identifiablePlugin?.PluginId);
            }
        }

        // 3. Return only matched plugin or manually select one of the matched plugins
        var allPlugins = _pluginManager.GetPlugins<IFilePlugin>().ToArray();

        if (matchedPlugins.Count == 1)
            return matchedPlugins.First();

        if (matchedPlugins.Count > 1)
            return await GetManualSelection(allPlugins, [.. matchedPlugins], SelectionStatus.MultipleMatches);

        // 5. If no plugin could identify the file, get manual feedback on all plugins that don't implement IIdentifyFiles
        if (loadInfo.AllowManualSelection)
            return await GetManualSelection(allPlugins, allPlugins.Where(x => !x.CanIdentifyFiles).ToArray(), SelectionStatus.NonIdentifiable);

        return null;
    }

    /// <summary>
    /// Identify a file with a single plugin.
    /// </summary>
    /// <param name="identifyFile">The plugin to identify with.</param>
    /// <param name="fileSystem">The file system to retrieve the file from.</param>
    /// <param name="filePath">The path of the file to identify.</param>
    /// <param name="streamManager">The stream manager.</param>
    /// <returns>If hte identification was successful.</returns>
    private async Task<bool> TryIdentifyFileAsync(IFilePlugin identifyFile, IFileSystem fileSystem, UPath filePath, IStreamManager streamManager)
    {
        // 1. Identify plugin
        var identifyContext = new IdentifyContext
        {
            TemporaryStreamManager = streamManager.CreateTemporaryStreamProvider()
        };
        var identifyResult = await identifyFile.AttemptIdentifyAsync(fileSystem, filePath, identifyContext);

        // 2. Close all streams opened by the identifying method
        streamManager.ReleaseAll();

        return identifyResult;
    }

    /// <summary>
    /// Select a plugin manually.
    /// </summary>
    /// <returns>The manually selected plugin.</returns>
    private async Task<IFilePlugin?> GetManualSelection(IFilePlugin[] allFilePlugins, IFilePlugin[] filteredFilePlugins, SelectionStatus status)
    {
        if (OnManualSelection == null)
            return null;

        // 1. Request manual selection by the user
        var selectionArgs = new ManualSelectionEventArgs(allFilePlugins, filteredFilePlugins, status);
        await OnManualSelection.Invoke(selectionArgs);

        return selectionArgs.Result;
    }

    /// <summary>
    /// Try to create a new plugin state.
    /// </summary>
    /// <param name="plugin">The plugin from which to create a new state.</param>
    /// <param name="fileManager">The plugin manager to pass to the state creation.</param>
    /// <param name="pluginState">The created state.</param>
    /// <param name="loadInfo">The load info for this loading operation.</param>
    /// <returns>If the creation was successful.</returns>
    private LoadResult TryCreateState(IFilePlugin plugin, IPluginFileManager fileManager, LoadFileOptions loadInfo, out IFilePluginState? pluginState)
    {
        pluginState = null;

        if (plugin.IsDeprecated)
        {
            loadInfo.Logger?.Warning("The plugin '{0}' is deprecated.", plugin.PluginId);

            return new LoadResult
            {
                Status = LoadStatus.Errored,
                Exception = new FilePluginDeprecatedException(plugin.Deprecated!),
                Reason = LoadErrorReason.Deprecated
            };
        }

        try
        {
            pluginState = plugin.CreatePluginState(fileManager);
        }
        catch (Exception e)
        {
            loadInfo.Logger?.Fatal(e, "The plugin state for '{0}' could not be initialized.", plugin.PluginId);

            return new LoadResult
            {
                Status = LoadStatus.Errored,
                Exception = e,
                Reason = LoadErrorReason.StateCreateError
            };
        }

        return new LoadResult
        {
            Status = LoadStatus.Successful,
            Reason = LoadErrorReason.None
        };
    }

    /// <summary>
    /// Try to load the state for the plugin.
    /// </summary>
    /// <param name="pluginState">The plugin state to load.</param>
    /// <param name="fileSystem">The file system to retrieve further files from.</param>
    /// <param name="filePath">The path of the identified file.</param>
    /// <param name="loadContext">The load context.</param>
    /// <param name="loadInfo">The load info for this loading operation.</param>
    /// <param name="plugin">The plugin from which the state should be loaded.</param>
    /// <returns>If the loading was successful.</returns>
    private async Task<LoadResult> TryLoadStateAsync(IFilePluginState pluginState, IFileSystem fileSystem, UPath filePath,
        LoadContext loadContext, LoadFileOptions loadInfo, IFilePlugin plugin)
    {
        // 1. Check if state supports loading
        if (!pluginState.CanLoad)
            return new LoadResult
            {
                Status = LoadStatus.Successful,
                Reason = LoadErrorReason.StateNoLoad
            };

        // 2. Try loading the state
        try
        {
            await Task.Run(async () => await pluginState.AttemptLoad(fileSystem, filePath, loadContext));
        }
        catch (Exception e)
        {
            loadInfo.Logger?.Fatal(e, "The plugin state for {0} could not be loaded.", plugin.PluginId);
            return new LoadResult
            {
                Status = LoadStatus.Errored,
                Exception = e,
                Reason = LoadErrorReason.StateLoadError
            };
        }

        return new LoadResult
        {
            Status = LoadStatus.Successful,
            Reason = LoadErrorReason.None
        };
    }
}