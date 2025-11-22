using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Management.Files;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Management.Files;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Management.Streams;
using Konnect.Contract.Plugin.File;
using Konnect.Extensions;
using Konnect.Management.Dialog;
using Konnect.Management.Streams;

namespace Konnect.Management.Files;

/// <summary>
/// Saves files loaded in the runtime of Kuriimu.
/// </summary>
class FileSaver : IFileSaver
{
    private readonly StreamMonitor _streamMonitor;

    public FileSaver(StreamMonitor streamMonitor)
    {
        _streamMonitor = streamMonitor;
    }

    /// <inheritdoc />
    public Task<SaveResult> SaveAsync(IFileState fileState, IFileSystem fileSystem, UPath savePath, SaveFileOptions saveInfo)
    {
        return SaveInternalAsync(fileState, fileSystem, savePath, saveInfo);
    }

    private async Task<SaveResult> SaveInternalAsync(IFileState fileState, IFileSystem destinationFileSystem, UPath savePath,
        SaveFileOptions saveInfo, bool isStart = true)
    {
        // 1. Check if state is saveable and if the contents are changed
        if (!fileState.PluginState.CanSave)
        {
            return new SaveResult
            {
                IsSuccessful = false,
                Reason = SaveErrorReason.SaveNotSupported
            };
        }

        if (!fileState.StateChanged)
        {
            return new SaveResult
            {
                IsSuccessful = false,
                Reason = SaveErrorReason.NoChanges
            };
        }

        // 2. Save child states
        foreach (var archiveChild in fileState.ArchiveChildren)
        {
            var childDestination = archiveChild.FileSystem.Clone(archiveChild.StreamManager);
            var saveChildResult = await SaveInternalAsync(archiveChild, childDestination, archiveChild.FilePath, saveInfo, false);
            if (saveChildResult is { IsSuccessful: false, Reason: not SaveErrorReason.NoChanges })
                return saveChildResult;
        }

        // 3. Save and replace state
        var saveAndReplaceResult = await SaveAndReplaceStateAsync(fileState, destinationFileSystem, savePath, saveInfo);
        if (!saveAndReplaceResult.IsSuccessful)
            return saveAndReplaceResult;

        // If this was not the first call into the save action, return a successful result
        if (!isStart)
            return new SaveResult
            {
                IsSuccessful = true,
                Reason = SaveErrorReason.None
            };

        // 4. Reload the current state and all its children
        var reloadResult = await ReloadInternalAsync(fileState, destinationFileSystem, savePath, saveInfo);
        return reloadResult;
    }

    private async Task<SaveResult> ReloadInternalAsync(IFileState fileState, IFileSystem destinationFileSystem, UPath savePath, SaveFileOptions saveInfo)
    {
        // 1. Reload current state
        var temporaryStreamProvider = fileState.StreamManager.CreateTemporaryStreamProvider();

        var dialogManager = new DialogManager(saveInfo.DialogManager, fileState.DialogOptions);
        var loadContext = new LoadContext
        {
            DialogManager = dialogManager,
            TemporaryStreamManager = temporaryStreamProvider,
            ProgressContext = saveInfo.Progress
        };
        var reloadResult = await TryLoadStateAsync(fileState.PluginState, destinationFileSystem, savePath.ToAbsolute(), loadContext);
        if (reloadResult.Status != LoadStatus.Successful)
            return new SaveResult
            {
                IsSuccessful = false,
                Exception = reloadResult.Exception,
                Reason = SaveErrorReason.StateReloadError
            };

        // 2. Set new file input, if state was loaded from a physical medium
        if (!fileState.HasParent)
            fileState.SetNewFileInput(destinationFileSystem, savePath);

        // 3. Reload all child states
        foreach (var archiveChild in fileState.ArchiveChildren)
        {
            var destination = archiveChild.FileSystem.Clone(archiveChild.StreamManager);
            var reloadChildResult = await ReloadInternalAsync(archiveChild, destination, archiveChild.FilePath, saveInfo);
            if (!reloadChildResult.IsSuccessful)
                return reloadChildResult;
        }

        return new SaveResult
        {
            IsSuccessful = true,
            Reason = SaveErrorReason.None
        };
    }

    private async Task<SaveResult> SaveAndReplaceStateAsync(IFileState fileState, IFileSystem destinationFileSystem, UPath savePath, SaveFileOptions saveInfo)
    {
        // 1. Save state to a temporary destination
        var temporaryContainer = _streamMonitor.CreateTemporaryFileSystem();
        var saveStateResult = await TrySaveState(fileState, temporaryContainer, savePath, saveInfo);
        if (!saveStateResult.IsSuccessful)
            return saveStateResult;

        // TODO: If reload fails then the original files get closed already, which makes future save actions impossible due to disposed streams

        // 2. Dispose of all streams in this state
        _streamMonitor.GetStreamManager(temporaryContainer).ReleaseAll();
        fileState.StreamManager.ReleaseAll();

        // 3. Replace files in destination file system
        var moveResult = await MoveFiles(fileState, temporaryContainer, destinationFileSystem);
        if (!moveResult.IsSuccessful)
            return moveResult;

        // 4. Release temporary destination
        _streamMonitor.ReleaseTemporaryFileSystem(temporaryContainer);

        return new SaveResult
        {
            IsSuccessful = true,
            Reason = SaveErrorReason.None
        };
    }

    /// <summary>
    /// Try to save the plugin state into a temporary destination.
    /// </summary>
    /// <param name="saveState">The plugin state to save.</param>
    /// <param name="temporaryContainer">The temporary destination the state will be saved in.</param>
    /// <param name="savePath">The path of the initial file to save.</param>
    /// <param name="saveInfo">The context for the save operation.</param>
    /// <returns>The result of the save state process.</returns>
    private async Task<SaveResult> TrySaveState(IFileState saveState, IFileSystem temporaryContainer, UPath savePath, SaveFileOptions saveInfo)
    {
        try
        {
            var saveContext = new SaveContext
            {
                ProgressContext = saveInfo.Progress
            };
            await Task.Run(async () => await saveState.PluginState.AttemptSave(temporaryContainer, savePath, saveContext));
        }
        catch (Exception ex)
        {
            saveInfo.Logger?.Fatal(ex, "The plugin state could not save.");

            return new SaveResult
            {
                IsSuccessful = false,
                Exception = ex,
                Reason = SaveErrorReason.StateSaveError
            };
        }

        return new SaveResult
        {
            IsSuccessful = true,
            Reason = SaveErrorReason.None
        };
    }

    /// <summary>
    /// Replace files in destination file system.
    /// </summary>
    /// <param name="fileState">The state to save in the destination.</param>
    /// <param name="sourceFileSystem">The file system to take the files from.</param>
    /// <param name="destinationFileSystem">The file system to replace the files in.</param>
    private async Task<SaveResult> MoveFiles(IFileState fileState, IFileSystem sourceFileSystem, IFileSystem destinationFileSystem)
    {
        if (fileState.HasParent)
        {
            // Put source filesystem into final destination
            var replaceResult = await TryReplaceFiles(sourceFileSystem, destinationFileSystem, fileState.ParentFileState.StreamManager);
            return replaceResult;
        }

        // Put source filesystem into final destination
        var copyResult = await TryCopyFiles(sourceFileSystem, destinationFileSystem);
        return copyResult;
    }

    /// <summary>
    /// Try to replace all saved files into the parent state.
    /// </summary>
    /// <param name="temporaryContainer"></param>
    /// <param name="destinationFileSystem"></param>
    /// <param name="stateStreamManager"></param>
    /// <returns>If the replacement was successful.</returns>
    private async Task<SaveResult> TryReplaceFiles(IFileSystem temporaryContainer, IFileSystem destinationFileSystem,
        IStreamManager stateStreamManager)
    {
        // 1. Check that all saved files exist in the parent filesystem already or can at least be created if missing
        foreach (var file in temporaryContainer.EnumerateAllFiles(UPath.Root))
        {
            if (!destinationFileSystem.FileExists(file) && !destinationFileSystem.CanCreateFiles)
                return new SaveResult
                {
                    IsSuccessful = false,
                    Reason = SaveErrorReason.DestinationNotExist
                };
        }

        // 2. Set new file data into parent file system
        foreach (var file in temporaryContainer.EnumerateAllFiles(UPath.Root))
        {
            try
            {
                var openedFile = await temporaryContainer.OpenFileAsync(file);
                destinationFileSystem.SetFileData(file, openedFile);

                stateStreamManager.Register(openedFile);
            }
            catch (Exception ex)
            {
                return new SaveResult
                {
                    IsSuccessful = false,
                    Exception = ex,
                    Reason = SaveErrorReason.FileReplaceError
                };
            }
        }

        return new SaveResult
        {
            IsSuccessful = true,
            Reason = SaveErrorReason.None
        };
    }

    /// <summary>
    /// Try to move all saved files into the destination path.
    /// </summary>
    /// <param name="temporaryContainer"></param>
    /// <param name="destinationFileSystem"></param>
    private async Task<SaveResult> TryCopyFiles(IFileSystem temporaryContainer, IFileSystem destinationFileSystem)
    {
        // 1. Set new file data into parent file system
        foreach (var file in temporaryContainer.EnumerateAllFiles(UPath.Root))
        {
            Stream saveData;

            try
            {
                saveData = await temporaryContainer.OpenFileAsync(file);
                destinationFileSystem.SetFileData(file, saveData);
            }
            catch (IOException ioe)
            {
                return new SaveResult
                {
                    IsSuccessful = false,
                    Exception = ioe,
                    Reason = SaveErrorReason.FileCopyError
                };
            }

            saveData.Close();
        }

        return new SaveResult
        {
            IsSuccessful = true,
            Reason = SaveErrorReason.None
        };
    }

    /// <summary>
    /// Try to load the state for the plugin.
    /// </summary>
    /// <param name="pluginState">The plugin state to load.</param>
    /// <param name="fileSystem">The file system to retrieve further files from.</param>
    /// <param name="savePath">The <see cref="savePath"/> for the initial file.</param>
    /// <param name="loadContext">The load context.</param>
    /// <returns>If the loading was successful.</returns>
    private async Task<LoadResult> TryLoadStateAsync(IFilePluginState pluginState, IFileSystem fileSystem, UPath savePath,
        LoadContext loadContext)
    {
        // 1. Check if state implements ILoadFile
        if (!pluginState.CanLoad)
            return new LoadResult
            {
                Status = LoadStatus.Errored,
                Reason = LoadErrorReason.StateNoLoad
            };

        // 2. Try loading the state
        try
        {
            await Task.Run(async () => await pluginState.AttemptLoad(fileSystem, savePath, loadContext));
        }
        catch (Exception ex)
        {
            return new LoadResult
            {
                Status = LoadStatus.Errored,
                Exception = ex,
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