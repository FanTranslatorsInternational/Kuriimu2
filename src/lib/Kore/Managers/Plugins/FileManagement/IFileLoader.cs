using System;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Models.FileSystem;
using Kontract.Models.Managers.Files;
using Kore.Models.LoadInfo;

namespace Kore.Managers.Plugins.FileManagement
{
    /// <summary>
    /// Exposes methods to load files into a plugin state.
    /// </summary>
    internal interface IFileLoader
    {
        /// <summary>
        /// An event to allow for manual selection by the user.
        /// </summary>
        event EventHandler<FileManager.ManualSelectionEventArgs> OnManualSelection;

        /// <summary>
        /// Loads any file from a given file system.
        /// </summary>
        /// <param name="fileSystem">The file system to load the file from.</param>
        /// <param name="filePath">The path into the file system.</param>
        /// <param name="loadInfo">The load context for this load action.</param>
        /// <returns>The loaded state of the file.</returns>
        Task<LoadResult> LoadAsync(IFileSystem fileSystem, UPath filePath, LoadInfo loadInfo);
    }
}
