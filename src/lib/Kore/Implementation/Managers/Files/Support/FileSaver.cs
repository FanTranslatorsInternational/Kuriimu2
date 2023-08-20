using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers.Files;
using Kontract.Interfaces.Managers.Streams;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Features;
using Kontract.Models.FileSystem;
using Kontract.Models.Managers.Files;
using Kontract.Models.Plugins.State;
using Kore.Implementation.Managers.Dialogs;
using Kore.Implementation.Managers.Streams;
using Kore.Models.Managers.Files;
using Kore.Models.Managers.Files.Support;
using Serilog;

namespace Kore.Implementation.Managers.Files.Support
{
    /// <summary>
    /// Saves files loaded in the runtime of Kuriimu.
    /// </summary>
    class FileSaver
    {
        private readonly StreamMonitor _streamMonitor;

        public FileSaver(StreamMonitor streamMonitor)
        {
            _streamMonitor = streamMonitor;
        }

        /// <summary>
        /// Saves a file from a file system.
        /// </summary>
        /// <param name="fileState">The loaded file to save.</param>
        /// <param name="fileSystem">The <see cref="IFileSystem"/> to save the file in.</param>
        /// <param name="savePath">The <see cref="UPath"/> to save the file at.</param>
        /// <param name="saveInfo">Additional information pertaining to saving a file.</param>
        /// <returns>The result of the save operation.</returns>
        public Task<KoreSaveResult> SaveAsync(IFileState fileState, IFileSystem fileSystem, UPath savePath, SaveInfo saveInfo)
        {
            return SaveInternalAsync(fileState, fileSystem, savePath, saveInfo);
        }

        private async Task<KoreSaveResult> SaveInternalAsync(IFileState fileState, IFileSystem destinationFileSystem, UPath savePath,
            SaveInfo saveInfo, bool isStart = true)
        {
            // 1. Check if state is saveable and if the contents are changed
            if (!fileState.PluginState.CanSave || !fileState.StateChanged)
                return new KoreSaveResult(SaveErrorReason.NoChanges);

            // 2. Save child states
            foreach (var archiveChild in fileState.ArchiveChildren)
            {
                var childDestination = archiveChild.FileSystem.Clone(archiveChild.StreamManager);
                var saveChildResult = await SaveInternalAsync(archiveChild, childDestination, archiveChild.FilePath, saveInfo, false);
                if (!saveChildResult.IsSuccessful)
                    return saveChildResult;
            }

            // 3. Save and replace state
            var saveAndReplaceResult = await SaveAndReplaceStateAsync(fileState, destinationFileSystem, savePath, saveInfo);
            if (saveAndReplaceResult != null)
                return saveAndReplaceResult;

            // If this was not the first call into the save action, return a successful result
            if (!isStart)
                return KoreSaveResult.Success;

            // 4. Reload the current state and all its children
            var reloadResult = await ReloadInternalAsync(fileState, destinationFileSystem, savePath, saveInfo);
            return reloadResult ?? KoreSaveResult.Success;
        }

        private async Task<KoreSaveResult> ReloadInternalAsync(IFileState fileState, IFileSystem destinationFileSystem, UPath savePath, SaveInfo saveInfo)
        {
            // 1. Reload current state
            var temporaryStreamProvider = fileState.StreamManager.CreateTemporaryStreamProvider();

            var internalDialogManager = new PredefinedDialogManager(saveInfo.DialogManager, fileState.DialogOptions);
            var loadContext = new LoadContext(temporaryStreamProvider, saveInfo.Progress, internalDialogManager);
            var reloadResult = await TryLoadStateAsync(fileState.PluginState, destinationFileSystem, savePath.ToAbsolute(), loadContext);
            if (reloadResult != null)
                return new KoreSaveResult(SaveErrorReason.StateReloadError, reloadResult.Exception);

            // 2. Set new file input, if state was loaded from a physical medium
            if (!fileState.HasParent)
                fileState.SetNewFileInput(destinationFileSystem, savePath);

            // 3. Reload all child states
            foreach (var archiveChild in fileState.ArchiveChildren)
            {
                var destination = archiveChild.FileSystem.Clone(archiveChild.StreamManager);
                var reloadChildResult = await ReloadInternalAsync(archiveChild, destination, archiveChild.FilePath, saveInfo);
                if (reloadChildResult != null)
                    return reloadChildResult;
            }

            return null;
        }

        private async Task<KoreSaveResult> SaveAndReplaceStateAsync(IFileState fileState, IFileSystem destinationFileSystem, UPath savePath, SaveInfo saveInfo)
        {
            // 1. Save state to a temporary destination
            var temporaryContainer = _streamMonitor.CreateTemporaryFileSystem();
            var saveStateResult = await TrySaveState(fileState.PluginState as ISaveFiles, temporaryContainer, savePath, saveInfo);
            if (saveStateResult != null)
                return saveStateResult;

            // TODO: If reload fails then the original files get closed already, which makes future save actions impossible due to disposed streams

            // 2. Dispose of all streams in this state
            _streamMonitor.GetStreamManager(temporaryContainer).ReleaseAll();
            fileState.StreamManager.ReleaseAll();

            // 3. Replace files in destination file system
            var moveResult = await MoveFiles(fileState, temporaryContainer, destinationFileSystem, saveInfo.Logger);
            if (moveResult != null)
                return moveResult;

            // 4. Release temporary destination
            _streamMonitor.ReleaseTemporaryFileSystem(temporaryContainer);

            return null;
        }

        /// <summary>
        /// Try to save the plugin state into a temporary destination.
        /// </summary>
        /// <param name="saveState">The plugin state to save.</param>
        /// <param name="temporaryContainer">The temporary destination the state will be saved in.</param>
        /// <param name="savePath">The path of the initial file to save.</param>
        /// <param name="saveInfo">The context for the save operation.</param>
        /// <returns>The result of the save state process.</returns>
        private async Task<KoreSaveResult> TrySaveState(ISaveFiles saveState, IFileSystem temporaryContainer, UPath savePath, SaveInfo saveInfo)
        {
            try
            {
                var saveContext = new SaveContext(saveInfo.Progress);
                await Task.Run(async () => await saveState.Save(temporaryContainer, savePath, saveContext));
            }
            catch (Exception ex)
            {
                return new KoreSaveResult(SaveErrorReason.StateSaveError, ex);
            }

            return null;
        }

        /// <summary>
        /// Replace files in destination file system.
        /// </summary>
        /// <param name="fileState">The state to save in the destination.</param>
        /// <param name="sourceFileSystem">The file system to take the files from.</param>
        /// <param name="destinationFileSystem">The file system to replace the files in.</param>
        /// <param name="logger"></param>
        private async Task<KoreSaveResult> MoveFiles(IFileState fileState, IFileSystem sourceFileSystem, IFileSystem destinationFileSystem, ILogger logger)
        {
            if (fileState.HasParent)
            {
                // Put source filesystem into final destination
                var replaceResult = await TryReplaceFiles(sourceFileSystem, destinationFileSystem, fileState.ParentFileState.StreamManager, logger);
                return replaceResult;
            }

            // Put source filesystem into final destination
            var copyResult = await TryCopyFiles(sourceFileSystem, destinationFileSystem, logger);
            return copyResult;
        }

        /// <summary>
        /// Try to replace all saved files into the parent state.
        /// </summary>
        /// <param name="temporaryContainer"></param>
        /// <param name="destinationFileSystem"></param>
        /// <param name="stateStreamManager"></param>
        /// <param name="logger"></param>
        /// <returns>If the replacement was successful.</returns>
        private async Task<KoreSaveResult> TryReplaceFiles(IFileSystem temporaryContainer, IFileSystem destinationFileSystem,
            IStreamManager stateStreamManager, ILogger logger)
        {
            var tempFiles = temporaryContainer.EnumerateAllFiles(UPath.Root).ToArray();

            // 1. Check that all saved files exist in the parent filesystem already or can be created if missing
            foreach (var file in tempFiles)
            {
                if (!destinationFileSystem.FileExists(file) && !destinationFileSystem.CanCreateFiles)
                {
                    logger.Error("File to replace: {0}", file);
                    return new KoreSaveResult(SaveErrorReason.DestinationNotExist);
                }
            }

            // 2. Set new file data into parent file system
            foreach (var file in tempFiles)
            {
                try
                {
                    var openedFile = await temporaryContainer.OpenFileAsync(file);
                    destinationFileSystem.SetFileData(file, openedFile);

                    stateStreamManager.Register(openedFile);
                }
                catch (Exception ex)
                {
                    logger.Error("File to replace: {0}", file);
                    return new KoreSaveResult(SaveErrorReason.FileReplaceError, ex);
                }
            }

            return null;
        }

        /// <summary>
        /// Try to move all saved files into the destination path.
        /// </summary>
        /// <param name="temporaryContainer"></param>
        /// <param name="destinationFileSystem"></param>
        /// <param name="logger"></param>
        private async Task<KoreSaveResult> TryCopyFiles(IFileSystem temporaryContainer, IFileSystem destinationFileSystem, ILogger logger)
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
                    logger.Error("File to copy: {0}", file);
                    return new KoreSaveResult(SaveErrorReason.FileCopyError, ioe);
                }

                saveData.Close();
            }

            return null;
        }

        /// <summary>
        /// Try to load the state for the plugin.
        /// </summary>
        /// <param name="pluginState">The plugin state to load.</param>
        /// <param name="fileSystem">The file system to retrieve further files from.</param>
        /// <param name="savePath">The <see cref="savePath"/> for the initial file.</param>
        /// <param name="loadContext">The load context.</param>
        /// <returns>If the loading was successful.</returns>
        private async Task<KoreLoadResult> TryLoadStateAsync(IPluginState pluginState, IFileSystem fileSystem, UPath savePath,
            LoadContext loadContext)
        {
            // 1. Check if state supports loading
            if (!pluginState.CanLoad)
                return new KoreLoadResult(LoadErrorReason.StateNoLoad);

            // 2. Try loading the state
            try
            {
                await Task.Run(async () => await pluginState.AttemptLoad(fileSystem, savePath, loadContext));
            }
            catch (Exception e)
            {
                return new KoreLoadResult(LoadErrorReason.StateLoadError, e);
            }

            return null;
        }
    }
}
