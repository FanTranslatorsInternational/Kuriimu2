using System;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Progress;
using Kontract.Interfaces.Providers;
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
        /// Provides access to file system creation.
        /// </summary>
        IFileSystemProvider FileSystemProvider { get; }

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

        /// <summary>
        /// Save a loaded state in place.
        /// </summary>
        /// <param name="stateInfo">The <see cref="IStateInfo"/> to save.</param>
        /// <returns></returns>
        Task<SaveResult> SaveFile(IStateInfo stateInfo);

        /// <summary>
        /// Save a loaded state to the given filesystem and path.
        /// </summary>
        /// <param name="stateInfo">The <see cref="IStateInfo"/> to save.</param>
        /// <param name="fileSystem">The filesystem at which to save the file.</param>
        /// <param name="savePath">The path into the filesystem to save the file at.</param>
        /// <returns></returns>
        Task<SaveResult> SaveFile(IStateInfo stateInfo, IFileSystem fileSystem, UPath savePath);

        /// <summary>
        /// Closes a loaded state.
        /// </summary>
        /// <param name="stateInfo">The state to close and release.</param>
        void Close(IStateInfo stateInfo);

        /// <summary>
        /// Closes all loaded states.
        /// </summary>
        void CloseAll();
    }
}
