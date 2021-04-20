using System;
using System.IO;
using System.Threading.Tasks;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;
using Kontract.Models.Context;
using Kontract.Models.IO;
using Kore.Factories;
using Kore.FileSystem.Implementations;
using Kore.Models;

namespace Kore.Managers.Plugins.FileManagement
{
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
        public Task<SaveResult> SaveAsync(IFileState fileState, IFileSystem fileSystem, UPath savePath, SaveInfo saveInfo)
        {
            return SaveInternalAsync(fileState, fileSystem, savePath, saveInfo);
        }

        private async Task<SaveResult> SaveInternalAsync(IFileState fileState, IFileSystem destinationFileSystem, UPath savePath,
            SaveInfo saveInfo, bool isStart = true)
        {
            // 1. Check if state is saveable and if the contents are changed
            if (!(fileState.PluginState.CanSave) || !fileState.StateChanged)
                return new SaveResult(true, "The file had no changes and was not saved.");

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
            if (!saveAndReplaceResult.IsSuccessful)
                return saveAndReplaceResult;

            // If this was not the first call into the save action, return a successful result
            if (!isStart)
                return SaveResult.SuccessfulResult;

            // 4. Reload the current state and all its children
            var reloadResult = await ReloadInternalAsync(fileState, destinationFileSystem, savePath, saveInfo);
            return reloadResult;
        }

        private async Task<SaveResult> ReloadInternalAsync(IFileState fileState, IFileSystem destinationFileSystem, UPath savePath, SaveInfo saveInfo)
        {
            // 1. Reload current state
            var temporaryStreamProvider = fileState.StreamManager.CreateTemporaryStreamProvider();

            var internalDialogManager = new InternalDialogManager(saveInfo.DialogManager, fileState.DialogOptions);
            var loadContext = new LoadContext(temporaryStreamProvider, saveInfo.Progress, internalDialogManager);
            var reloadResult = await TryLoadStateAsync(fileState.PluginState, destinationFileSystem, savePath.ToAbsolute(), loadContext);
            if (!reloadResult.IsSuccessful)
                return new SaveResult(reloadResult.Exception);

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

            return SaveResult.SuccessfulResult;
        }

        private async Task<SaveResult> SaveAndReplaceStateAsync(IFileState fileState, IFileSystem destinationFileSystem, UPath savePath, SaveInfo saveInfo)
        {
            // 1. Save state to a temporary destination
            var temporaryContainer = _streamMonitor.CreateTemporaryFileSystem();
            var saveStateResult = await TrySaveState(fileState.PluginState as ISaveFiles, temporaryContainer, savePath, saveInfo);
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

            return SaveResult.SuccessfulResult;
        }

        /// <summary>
        /// Try to save the plugin state into a temporary destination.
        /// </summary>
        /// <param name="saveState">The plugin state to save.</param>
        /// <param name="temporaryContainer">The temporary destination the state will be saved in.</param>
        /// <param name="savePath">The path of the initial file to save.</param>
        /// <param name="saveInfo">The context for the save operation.</param>
        /// <returns>The result of the save state process.</returns>
        private async Task<SaveResult> TrySaveState(ISaveFiles saveState, IFileSystem temporaryContainer, UPath savePath, SaveInfo saveInfo)
        {
            try
            {
                var saveContext = new SaveContext(saveInfo.Progress);
                await Task.Run(async () => await saveState.Save(temporaryContainer, savePath, saveContext));
            }
            catch (Exception ex)
            {
                return new SaveResult(ex);
            }

            return SaveResult.SuccessfulResult;
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
                destinationFileSystem = new SubFileSystem(destinationFileSystem, fileState.FilePath.ToAbsolute().GetDirectory());

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
            foreach (var file in temporaryContainer.EnumeratePaths(UPath.Root))
            {
                if (!destinationFileSystem.FileExists(file) && !destinationFileSystem.CanCreateFiles)
                    return new SaveResult(false, $"'{file}' did not exist in '{destinationFileSystem.ConvertPathToInternal(UPath.Root)}'.");
            }

            // 2. Set new file data into parent file system
            foreach (var file in temporaryContainer.EnumeratePaths(UPath.Root))
            {
                try
                {
                    var openedFile = await temporaryContainer.OpenFileAsync(file);
                    destinationFileSystem.SetFileData(file, openedFile);

                    stateStreamManager.Register(openedFile);
                }
                catch (Exception ex)
                {
                    return new SaveResult(ex);
                }
            }

            return SaveResult.SuccessfulResult;
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
                    return new SaveResult(ioe);
                }

                saveData.Close();
            }

            return SaveResult.SuccessfulResult;
        }

        /// <summary>
        /// Try to load the state for the plugin.
        /// </summary>
        /// <param name="pluginState">The plugin state to load.</param>
        /// <param name="fileSystem">The file system to retrieve further files from.</param>
        /// <param name="savePath">The <see cref="savePath"/> for the initial file.</param>
        /// <param name="loadContext">The load context.</param>
        /// <returns>If the loading was successful.</returns>
        private async Task<LoadResult> TryLoadStateAsync(IPluginState pluginState, IFileSystem fileSystem, UPath savePath,
            LoadContext loadContext)
        {
            // 1. Check if state implements ILoadFile
            if (!pluginState.CanLoad)
                return new LoadResult(false, "The state is not loadable.");

            // 2. Try loading the state
            try
            {
                await Task.Run(async () => await pluginState.TryLoad(fileSystem, savePath, loadContext));
            }
            catch (Exception ex)
            {
                return new LoadResult(ex);
            }

            return new LoadResult(true);
        }
    }
}
