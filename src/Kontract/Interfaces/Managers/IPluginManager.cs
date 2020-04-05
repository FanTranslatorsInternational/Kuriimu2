using System;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Progress;
using Kontract.Models;
using Kontract.Models.IO;

namespace Kontract.Interfaces.Managers
{
    /// <summary>
    /// Exposes methods to load files in the Kuriimu runtime.
    /// </summary>
    public interface IPluginManager
    {
        /// <summary>
        /// An event to allow for manual selection by the user.
        /// </summary>
        event EventHandler<ManualSelectionEventArgs> OnManualSelection;

        /// <summary>
        /// Declares if manual plugin selection on Load is allowed.
        /// </summary>
        bool AllowManualSelection { get; set; }

        /// <summary>
        /// Loads a file from a given file system.
        /// </summary>
        /// <param name="fileSystem">The file system to load the file from.</param>
        /// <param name="path">The file to load from the file system.</param>
        /// <param name="progress">The context to report progress.</param>
        /// <returns>The loaded <see cref="IStateInfo"/> for the file.</returns>
        Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, IProgressContext progress = null);

        /// <summary>
        /// Loads a file from a given file system.
        /// </summary>
        /// <param name="fileSystem">The file system to load the file from.</param>
        /// <param name="path">The file to load from the file system.</param>
        /// <param name="pluginId">The Id of the plugin to load the file with.</param>
        /// <param name="progress">The context to report progress.</param>
        /// <returns>The loaded <see cref="IStateInfo"/> for the file.</returns>
        Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, Guid pluginId, IProgressContext progress = null);

        Task<SaveResult> SaveFile(IStateInfo stateInfo);
    }
}
