using System;
using System.IO;
using System.Threading.Tasks;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Progress;
using Kontract.Interfaces.Providers;
using Kontract.Models;
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
        private readonly IProgressContext _progress;

        public FileSaver(IProgressContext progress)
        {
            _progress = progress;
        }

        /// <inheritdoc />
        public Task<SaveResult> SaveAsync(IStateInfo stateInfo, UPath savePath)
        {
            var destination = stateInfo.ParentStateInfo == null ?
                FileSystemFactory.CreatePhysicalFileSystem(savePath.GetDirectory(), stateInfo.StreamManager) :
                stateInfo.FileSystem;

            return SaveInternal(stateInfo, destination, savePath);
        }

        /// <inheritdoc />
        public Task<SaveResult> SaveAsync(IStateInfo stateInfo, IFileSystem fileSystem, UPath savePath)
        {
            return SaveInternal(stateInfo, fileSystem, savePath);
        }

        private async Task<SaveResult> SaveInternal(IStateInfo stateInfo, IFileSystem destinationFileSystem, UPath savePath)
        {
            // 1. Check if state is saveable and if the contents are changed
            if (!(stateInfo.State is ISaveFiles saveState) || !stateInfo.StateChanged)
                return new SaveResult(true, "The file had no changes and was not saved.");

            // 2. Save child states
            foreach (var archiveChild in stateInfo.ArchiveChildren)
            {
                var saveChildResult = await SaveAsync(archiveChild, archiveChild.FilePath);
                if (!saveChildResult.IsSuccessful)
                    return saveChildResult;
            }

            // 3. Save state to a temporary destination
            var temporaryContainer = CreateTemporaryFileSystem(stateInfo.StreamManager);
            var saveStateResult = await TrySaveState(saveState, temporaryContainer, savePath.GetName());
            if (!saveStateResult.IsSuccessful)
                return saveStateResult;

            // TODO: If reload fails then the original files get closed already, which makes future save actions impossible due to disposed streams

            // 4. Dispose of all streams in this state
            stateInfo.StreamManager.ReleaseAll();

            if (stateInfo.ParentStateInfo != null)
            {
                // 5. Put temporary filesystem into final destination
                temporaryContainer = temporaryContainer.Clone(stateInfo.ParentStateInfo.StreamManager);
                var subDestinationFileSystem = new SubFileSystem(destinationFileSystem, stateInfo.FilePath.ToAbsolute().GetDirectory());
                var replaceResult = await TryReplaceFilesInParentAsync(temporaryContainer, subDestinationFileSystem);
                if (!replaceResult.IsSuccessful)
                    return replaceResult;

                // 6. Load the initial file from the parent state
                var temporaryStreamProvider = stateInfo.StreamManager.CreateTemporaryStreamProvider();
                var reloadResult = await TryLoadStateAsync(stateInfo.State, subDestinationFileSystem, savePath,
                    temporaryStreamProvider);
                if (!reloadResult.IsSuccessful)
                    return new SaveResult(reloadResult.Exception);

                return SaveResult.SuccessfulResult;
            }
            else
            {
                // 5. Put temporary filesystem into final destination
                temporaryContainer = temporaryContainer.Clone(stateInfo.StreamManager);
                var copyResult = await TryCopyFiles(temporaryContainer, destinationFileSystem);
                if (!copyResult.IsSuccessful)
                    return copyResult;

                // 6. Load the initial file from the physical destination
                var temporaryStreamProvider = stateInfo.StreamManager.CreateTemporaryStreamProvider();
                var reloadResult = await TryLoadStateAsync(stateInfo.State, destinationFileSystem, savePath.GetName(),
                    temporaryStreamProvider);
                if (!reloadResult.IsSuccessful)
                    return new SaveResult(reloadResult.Exception);

                // 7. Use file path and file system in newly loaded state
                stateInfo.SetNewFileInput(destinationFileSystem, savePath.GetName());

                return SaveResult.SuccessfulResult;
            }
        }

        /// <summary>
        /// Create a temporary file system to save the plugin into.
        /// </summary>
        /// <param name="streamManager">The stream manager for that file system.</param>
        /// <returns>The temporary destination.</returns>
        private IFileSystem CreateTemporaryFileSystem(IStreamManager streamManager)
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var tempDirectory = Path.Combine(baseDirectory, "tmp/" + Guid.NewGuid().ToString("D"));

            return FileSystemFactory.CreatePhysicalFileSystem(new UPath(tempDirectory), streamManager);
        }

        /// <summary>
        /// Try to save the plugin state into a temporary destination.
        /// </summary>
        /// <param name="saveState">The plugin state to save.</param>
        /// <param name="temporaryContainer">The temporary destination the state will be saved in.</param>
        /// <param name="fileName">The name of the initial file.</param>
        /// <returns>The result of the save state process.</returns>
        private async Task<SaveResult> TrySaveState(ISaveFiles saveState, IFileSystem temporaryContainer, string fileName)
        {
            try
            {
                await saveState.Save(temporaryContainer, fileName, _progress);
            }
            catch (Exception ex)
            {
                return new SaveResult(ex);
            }

            return SaveResult.SuccessfulResult;
        }

        /// <summary>
        /// Try to replace all saved files into the parent state.
        /// </summary>
        /// <param name="temporaryContainer"></param>
        /// <param name="destinationFileSystem"></param>
        /// <returns>If the replacement was successful.</returns>
        private async Task<SaveResult> TryReplaceFilesInParentAsync(IFileSystem temporaryContainer,
            IFileSystem destinationFileSystem)
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
        /// <param name="temporaryStreamProvider">The stream provider for temporary files.</param>
        /// <returns>If the loading was successful.</returns>
        private async Task<LoadResult> TryLoadStateAsync(IPluginState pluginState, IFileSystem fileSystem, UPath savePath, ITemporaryStreamProvider temporaryStreamProvider)
        {
            // 1. Check if state implements ILoadFile
            if (!(pluginState is ILoadFiles loadableState))
                return new LoadResult(false, "The state is not loadable.");

            // 2. Try loading the state
            try
            {
                await loadableState.Load(fileSystem, savePath, temporaryStreamProvider, _progress);
            }
            catch (Exception ex)
            {
                return new LoadResult(ex);
            }

            return new LoadResult(true);
        }
    }
}
