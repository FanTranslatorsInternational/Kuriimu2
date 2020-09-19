using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Progress;
using Kontract.Models;
using Kontract.Models.Archive;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace Kontract.Interfaces.Managers
{
    /// <summary>
    /// Exposes methods to load files in the Kuriimu runtime.
    /// </summary>
    public interface IPluginManager
    {
        #region Load File

        #region Load FileSystem

        /// <summary>
        /// Loads a file from a given file system.
        /// </summary>
        /// <param name="fileSystem">The file system to load the file from.</param>
        /// <param name="path">The file to load from the file system.</param>
        /// <returns>The loaded <see cref="IStateInfo"/> for the file.</returns>
        Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path);

        /// <summary>
        /// Loads a file from a given file system.
        /// </summary>
        /// <param name="fileSystem">The file system to load the file from.</param>
        /// <param name="path">The file to load from the file system.</param>
        /// <param name="loadFileContext">The context with additional parameters for the load process.</param>
        /// <returns>The loaded <see cref="IStateInfo"/> for the file.</returns>
        Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, LoadFileContext loadFileContext);

        /// <summary>
        /// Loads a file from a given file system.
        /// </summary>
        /// <param name="fileSystem">The file system to load the file from.</param>
        /// <param name="path">The file to load from the file system.</param>
        /// <param name="pluginId">The Id of the plugin to load the file with.</param>
        /// <returns>The loaded <see cref="IStateInfo"/> for the file.</returns>
        Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, Guid pluginId);

        /// <summary>
        /// Loads a file from a given file system.
        /// </summary>
        /// <param name="fileSystem">The file system to load the file from.</param>
        /// <param name="path">The file to load from the file system.</param>
        /// <param name="pluginId">The Id of the plugin to load the file with.</param>
        /// <param name="loadFileContext">The context with additional parameters for the load process.</param>
        /// <returns>The loaded <see cref="IStateInfo"/> for the file.</returns>
        Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, Guid pluginId, LoadFileContext loadFileContext);

        #endregion

        #region Load IArchiveFileInfo

        /// <summary>
        /// Loads a virtual path into the Kuriimu runtime.
        /// </summary>
        /// <param name="stateInfo">The loaded path state to load a path from.</param>
        /// <param name="afi">The path to load from that state.</param>
        /// <returns>The loaded state of the path.</returns>
        Task<LoadResult> LoadFile(IStateInfo stateInfo, IArchiveFileInfo afi);

        /// <summary>
        /// Loads a virtual path into the Kuriimu runtime.
        /// </summary>
        /// <param name="stateInfo">The loaded path state to load a path from.</param>
        /// <param name="afi">The path to load from that state.</param>
        /// <param name="loadFileContext">The context with additional parameters for the load process.</param>
        /// <returns>The loaded state of the path.</returns>
        Task<LoadResult> LoadFile(IStateInfo stateInfo, IArchiveFileInfo afi, LoadFileContext loadFileContext);

        /// <summary>
        /// Loads a virtual path into the Kuriimu runtime.
        /// </summary>
        /// <param name="stateInfo">The loaded path state to load a path from.</param>
        /// <param name="afi">The path to load from that state.</param>
        /// <param name="pluginId">The plugin to load this virtual file with.</param>
        /// <returns>The loaded state of the path.</returns>
        Task<LoadResult> LoadFile(IStateInfo stateInfo, IArchiveFileInfo afi, Guid pluginId);

        /// <summary>
        /// Loads a virtual path into the Kuriimu runtime.
        /// </summary>
        /// <param name="stateInfo">The loaded path state to load a path from.</param>
        /// <param name="afi">The path to load from that state.</param>
        /// <param name="pluginId">The plugin to load this virtual file with.</param>
        /// <param name="loadFileContext">The context with additional parameters for the load process.</param>
        /// <returns>The loaded state of the path.</returns>
        Task<LoadResult> LoadFile(IStateInfo stateInfo, IArchiveFileInfo afi, Guid pluginId, LoadFileContext loadFileContext);

        #endregion

        #endregion

        #region Save File

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

        #endregion

        #region Close File

        /// <summary>
        /// Closes a loaded state.
        /// </summary>
        /// <param name="stateInfo">The state to close and release.</param>
        void Close(IStateInfo stateInfo);

        /// <summary>
        /// Closes all loaded states.
        /// </summary>
        void CloseAll();

        #endregion
    }
}
