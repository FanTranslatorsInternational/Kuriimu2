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

        // TODO: Communicate errors properly
        /// <inheritdoc />
        public async Task SaveAsync(IStateInfo stateInfo, UPath savePath)
        {
            // 1. Check if state is saveable and if the contents are changed
            if (!(stateInfo.State is ISaveFiles saveState) || !stateInfo.StateChanged)
                return;

            // 2. Save child states
            foreach (var archiveChild in stateInfo.ArchiveChildren)
                await SaveAsync(archiveChild, archiveChild.FilePath);

            // 3. Save state to a temporary destination
            var streamManager = stateInfo.StreamManager;
            var temporaryContainer = CreateTemporaryFileSystem(streamManager);
            if (!await TrySaveState(saveState, temporaryContainer, savePath.GetName()))
            {
                streamManager.ReleaseAll();

                // TODO: Handle errors
                return;
            }

            // 4. Dispose of all streams in this state
            streamManager.ReleaseAll();

            if (stateInfo.ParentStateInfo != null)
            {
                // 5. Put the temporary destination into the parent state
                if (!await TryReplaceFilesInParentAsync(stateInfo, temporaryContainer))
                    return;

                // 6. Load the initial file from the parent state
                var temporaryStreamProvider = stateInfo.StreamManager.CreateTemporaryStreamProvider();
                if (!await TryLoadStateAsync(stateInfo.State, stateInfo.FileSystem, savePath, temporaryStreamProvider))
                    ;
            }
            else
            {
                // 5. Move files to final destination
                var destinationFileSystem = FileSystemFactory.CreatePhysicalFileSystem(savePath.GetDirectory(), streamManager);
                if (!await TryCopyFiles(temporaryContainer, destinationFileSystem))
                    return;

                // 6. Load the initial file from the parent state
                var temporaryStreamProvider = stateInfo.StreamManager.CreateTemporaryStreamProvider();
                if (!await TryLoadStateAsync(stateInfo.State, destinationFileSystem, savePath.GetName(), temporaryStreamProvider))
                    ;
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
        /// <returns>If the save process was successful.</returns>
        private async Task<bool> TrySaveState(ISaveFiles saveState, IFileSystem temporaryContainer, string fileName)
        {
            try
            {
                await Task.Factory.StartNew(() => saveState.Save(temporaryContainer, fileName, _progress));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Try to replace all saved files into the parent state.
        /// </summary>
        /// <param name="stateInfo"></param>
        /// <param name="temporaryContainer"></param>
        /// <returns>If the replacement was successful.</returns>
        private async Task<bool> TryReplaceFilesInParentAsync(IStateInfo stateInfo, IFileSystem temporaryContainer)
        {
            var parentFileSystem = stateInfo.FileSystem;

            // 1. Check that all saved files exist in the parent filesystem already
            foreach (var file in temporaryContainer.EnumeratePaths(UPath.Root))
            {
                var parentPath = UPath.Combine(stateInfo.SubPath, file);
                if (!parentFileSystem.FileExists(parentPath))
                    return false;
            }

            // 2. Set new file data into parent file system
            foreach (var file in temporaryContainer.EnumeratePaths(UPath.Root))
            {
                var parentPath = UPath.Combine(stateInfo.SubPath, file);
                var openedFile = await temporaryContainer.OpenFileAsync(file);
                parentFileSystem.SetFileData(parentPath, openedFile);
            }

            return true;
        }

        /// <summary>
        /// Try to move all saved files into the destination path.
        /// </summary>
        /// <param name="temporaryContainer"></param>
        /// <param name="destinationFileSystem"></param>
        private async Task<bool> TryCopyFiles(IFileSystem temporaryContainer, IFileSystem destinationFileSystem)
        {
            // 1. Set new file data into parent file system
            foreach (var file in temporaryContainer.EnumeratePaths(UPath.Root))
            {
                var saveData = await temporaryContainer.OpenFileAsync(file);
                destinationFileSystem.SetFileData(file, saveData);
                saveData.Close();
            }

            return true;
        }

        /// <summary>
        /// Try to load the state for the plugin.
        /// </summary>
        /// <param name="pluginState">The plugin state to load.</param>
        /// <param name="fileSystem">The file system to retrieve further files from.</param>
        /// <param name="savePath">The <see cref="savePath"/> for the initial file.</param>
        /// <param name="temporaryStreamProvider">The stream provider for temporary files.</param>
        /// <returns>If the loading was successful.</returns>
        private async Task<bool> TryLoadStateAsync(IPluginState pluginState, IFileSystem fileSystem, UPath savePath, ITemporaryStreamProvider temporaryStreamProvider)
        {
            // 1. Check if state implements ILoadFile
            if (!(pluginState is ILoadFiles loadableState))
            {
                return false;
            }

            // 2. Try loading the state
            try
            {
                await Task.Factory.StartNew(() => loadableState.Load(fileSystem, savePath, temporaryStreamProvider, _progress));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
