using System;
using System.IO;
using System.Threading.Tasks;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Progress;
using Kontract.Models;
using Kontract.Models.Context;
using Kontract.Models.IO;
using Kore.Factories;
using Kore.FileSystem.Implementations;

namespace Kore.Managers.Plugins.FileManagement
{
    /// <summary>
    /// Saves files loaded in the runtime of Kuriimu.
    /// </summary>
    class FileSaver : IFileSaver
    {
        private readonly StreamMonitor _streamMonitor;
        private readonly IDialogManager _dialogManager;

        public FileSaver(StreamMonitor streamMonitor, IDialogManager dialogManager)
        {
            _streamMonitor = streamMonitor;
            _dialogManager = dialogManager;
        }

        /// <inheritdoc />
        public Task<SaveResult> SaveAsync(IStateInfo stateInfo, UPath savePath, IProgressContext progress)
        {
            var destination = CreateDestinationFileSystem(stateInfo, savePath);
            return SaveAsync(stateInfo, destination, savePath, progress);
        }

        /// <inheritdoc />
        public Task<SaveResult> SaveAsync(IStateInfo stateInfo, IFileSystem fileSystem, UPath savePath, IProgressContext progress)
        {
            return SaveInternalAsync(stateInfo, fileSystem, savePath, progress);
        }

        private async Task<SaveResult> SaveInternalAsync(IStateInfo stateInfo, IFileSystem destinationFileSystem, UPath savePath,
            IProgressContext progress, bool isStart = true)
        {
            // 1. Check if state is saveable and if the contents are changed
            if (!(stateInfo.PluginState is ISaveFiles) || !stateInfo.StateChanged)
                return new SaveResult(true, "The file had no changes and was not saved.");

            // 2. Save child states
            foreach (var archiveChild in stateInfo.ArchiveChildren)
            {
                var destination = CreateDestinationFileSystem(archiveChild, archiveChild.FilePath);
                var saveChildResult = await SaveInternalAsync(archiveChild, destination, archiveChild.FilePath, progress, false);
                if (!saveChildResult.IsSuccessful)
                    return saveChildResult;
            }

            // 3. Save and replace state
            var saveAndReplaceResult = await SaveAndReplaceStateAsync(stateInfo, destinationFileSystem, savePath, progress);
            if (!saveAndReplaceResult.IsSuccessful)
                return saveAndReplaceResult;

            // If this was not the first call into the save action, return a successful result
            if (!isStart)
                return SaveResult.SuccessfulResult;

            // 4. Reload the current state and all its children
            var reloadResult = await ReloadInternalAsync(stateInfo, destinationFileSystem, savePath, progress);
            return reloadResult;
        }

        private async Task<SaveResult> ReloadInternalAsync(IStateInfo stateInfo, IFileSystem destinationFileSystem, UPath savePath,
            IProgressContext progress)
        {
            // 1. Reload current state
            var temporaryStreamProvider = stateInfo.StreamManager.CreateTemporaryStreamProvider();
            savePath = stateInfo.HasParent ? savePath : savePath.GetName();

            var internalDialogManager = new InternalDialogManager(_dialogManager, stateInfo.DialogOptions);
            var loadContext = new LoadContext(temporaryStreamProvider, progress, internalDialogManager);
            var reloadResult = await TryLoadStateAsync(stateInfo.PluginState, destinationFileSystem, savePath, loadContext);
            if (!reloadResult.IsSuccessful)
                return new SaveResult(reloadResult.Exception);

            // 2. Set new file input, if state was loaded from a physical medium
            if (!stateInfo.HasParent)
                stateInfo.SetNewFileInput(destinationFileSystem, savePath);

            // 3. Reload all child states
            foreach (var archiveChild in stateInfo.ArchiveChildren)
            {
                var destination = CreateDestinationFileSystem(archiveChild, archiveChild.FilePath);
                var reloadChildResult = await ReloadInternalAsync(archiveChild, destination, archiveChild.FilePath, progress);
                if (!reloadChildResult.IsSuccessful)
                    return reloadChildResult;
            }

            return SaveResult.SuccessfulResult;
        }

        private async Task<SaveResult> SaveAndReplaceStateAsync(IStateInfo stateInfo, IFileSystem destinationFileSystem, UPath savePath,
            IProgressContext progress)
        {
            var saveState = stateInfo.PluginState as ISaveFiles;

            // 1. Save state to a temporary destination
            var temporaryContainer = _streamMonitor.CreateTemporaryFileSystem();
            var saveStateResult = await TrySaveState(saveState, temporaryContainer, savePath.GetName(), progress);
            if (!saveStateResult.IsSuccessful)
                return saveStateResult;

            // TODO: If reload fails then the original files get closed already, which makes future save actions impossible due to disposed streams

            // 2. Dispose of all streams in this state
            _streamMonitor.GetStreamManager(temporaryContainer).ReleaseAll();
            stateInfo.StreamManager.ReleaseAll();

            // 3. Replace files in destination file system
            var moveResult = await MoveFiles(stateInfo, temporaryContainer, destinationFileSystem);
            if (!moveResult.IsSuccessful)
                return moveResult;

            // 4. Release temporary destination
            _streamMonitor.ReleaseTemporaryFileSystem(temporaryContainer);

            return SaveResult.SuccessfulResult;
        }

        /// <summary>
        /// Creates an <see cref="IFileSystem"/> to save the files to.
        /// </summary>
        /// <param name="stateInfo">The state from which to create the file system.</param>
        /// <param name="savePath">The path for the root destination.</param>
        /// <returns></returns>
        private IFileSystem CreateDestinationFileSystem(IStateInfo stateInfo, UPath savePath)
        {
            return stateInfo.FilePath == savePath ?
                stateInfo.FileSystem.Clone(stateInfo.StreamManager) :
                FileSystemFactory.CreatePhysicalFileSystem(savePath.GetDirectory(), stateInfo.StreamManager);
        }

        /// <summary>
        /// Try to save the plugin state into a temporary destination.
        /// </summary>
        /// <param name="saveState">The plugin state to save.</param>
        /// <param name="temporaryContainer">The temporary destination the state will be saved in.</param>
        /// <param name="fileName">The name of the initial file.</param>
        /// <param name="progress">The context to report progress.</param>
        /// <returns>The result of the save state process.</returns>
        private async Task<SaveResult> TrySaveState(ISaveFiles saveState, IFileSystem temporaryContainer, string fileName,
            IProgressContext progress)
        {
            try
            {
                var saveContext = new SaveContext(progress);
                await Task.Run(async () => await saveState.Save(temporaryContainer, fileName, saveContext));
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
        /// <param name="stateInfo">The state to save in the destination.</param>
        /// <param name="sourceFileSystem">The file system to take the files from.</param>
        /// <param name="destinationFileSystem">The file system to replace the files in.</param>
        private async Task<SaveResult> MoveFiles(IStateInfo stateInfo, IFileSystem sourceFileSystem, IFileSystem destinationFileSystem)
        {
            if (stateInfo.HasParent)
            {
                // Put source filesystem into final destination
                destinationFileSystem = new SubFileSystem(destinationFileSystem, stateInfo.FilePath.ToAbsolute().GetDirectory());

                var replaceResult = await TryReplaceFiles(sourceFileSystem, destinationFileSystem, stateInfo.ParentStateInfo.StreamManager);
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
            foreach (var file in temporaryContainer.EnumeratePaths(UPath.Root))
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
            if (!(pluginState is ILoadFiles loadableState))
                return new LoadResult(false, "The state is not loadable.");

            // 2. Try loading the state
            try
            {
                await Task.Run(async () => await loadableState.Load(fileSystem, savePath, loadContext));
            }
            catch (Exception ex)
            {
                return new LoadResult(ex);
            }

            return new LoadResult(true);
        }
    }
}
