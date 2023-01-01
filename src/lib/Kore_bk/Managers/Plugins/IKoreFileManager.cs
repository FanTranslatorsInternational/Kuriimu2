using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kontract.Interfaces.Managers.Files;
using Kontract.Interfaces.Plugins.Entry;
using Kontract.Interfaces.Plugins.Loaders;
using Kontract.Models.Managers.Files;
using Kontract.Models.Plugins.Loaders;
using Serilog;
using Zio;

namespace Kore.Managers.Plugins
{
    /// <summary>
    /// Exposes methods to load physical and virtual files directly.
    /// </summary>
    public interface IKoreFileManager : IFileManager, IDisposable
    {
        /// <summary>
        /// An event to allow for manual selection by the user.
        /// </summary>
        event EventHandler<FileManager.ManualSelectionEventArgs> OnManualSelection;

        /// <summary>
        /// The logger for this plugin manager.
        /// </summary>
        ILogger Logger { get; set; }

        /// <summary>
        /// Declares if manual plugin selection on Load is allowed.
        /// </summary>
        bool AllowManualSelection { get; set; }

        /// <summary>
        /// The errors the plugins produced when loaded.
        /// </summary>
        IReadOnlyList<PluginLoadError> LoadErrors { get; }

        #region Get Methods

        /// <summary>
        /// Gets the <see cref="IFileState"/> of the requested file.
        /// </summary>
        /// <param name="filePath">The path of the file to request.</param>
        /// <returns>The <see cref="IFileState"/> of the file.</returns>
        IFileState GetLoadedFile(UPath filePath);

        /// <summary>
        /// Retrieves all <see cref="IPluginLoader"/>s that can load files.
        /// </summary>
        /// <returns></returns>
        IPluginLoader<IFilePlugin>[] GetFilePluginLoaders();

        /// <summary>
        /// Retrieves all <see cref="IPluginLoader"/>s that can render game previews.
        /// </summary>
        /// <returns></returns>
        IPluginLoader<IGameAdapter>[] GetGamePluginLoaders();

        #endregion

        #region Identify file

        /// <summary>
        /// Identifies a stream against a given plugin.
        /// </summary>
        /// <param name="file">The physical file to identify.</param>
        /// <param name="pluginId">The plugin ID to identify with.</param>
        /// <returns>If the file could be identified by the denoted plugin.</returns>
        Task<bool> CanIdentify(string file, Guid pluginId);

        #endregion

        #region Load File

        #region Load Physical

        /// <summary>
        /// Loads a physical path into the Kuriimu runtime.
        /// </summary>
        /// <param name="file">The path to the path to load.</param>
        /// <returns>The loaded state of the path.</returns>
        Task<LoadResult> LoadFile(string file);

        /// <summary>
        /// Loads a physical path into the Kuriimu runtime.
        /// </summary>
        /// <param name="file">The path to the path to load.</param>
        /// <param name="pluginId">the plugin with which to load the file.</param>
        /// <returns>The loaded state of the path.</returns>
        Task<LoadResult> LoadFile(string file, Guid pluginId);

        /// <summary>
        /// Loads a physical path into the Kuriimu runtime.
        /// </summary>
        /// <param name="file">The path to the path to load.</param>
        /// <param name="loadFileContext">The context with additional parameters for the load process.</param>
        /// <returns>The loaded state of the path.</returns>
        Task<LoadResult> LoadFile(string file, LoadFileContext loadFileContext);

        #endregion

        #region Load FileSystem

        /// <summary>
        /// Loads a file from a given file system.
        /// </summary>
        /// <param name="fileSystem">The file system to load the file from.</param>
        /// <param name="path">The file to load from the file system.</param>
        /// <param name="parentFileState">The state from which the file system originates.</param>
        /// <returns>The loaded <see cref="IFileState"/> for the file.</returns>
        Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, IFileState parentFileState);

        /// <summary>
        /// Loads a file from a given file system.
        /// </summary>
        /// <param name="fileSystem">The file system to load the file from.</param>
        /// <param name="path">The file to load from the file system.</param>
        /// <param name="pluginId">The Id of the plugin to load the file with.</param>
        /// <param name="parentFileState">The state from which the file system originates.</param>
        /// <returns>The loaded <see cref="IFileState"/> for the file.</returns>
        Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, Guid pluginId, IFileState parentFileState);

        /// <summary>
        /// Loads a file from a given file system.
        /// </summary>
        /// <param name="fileSystem">The file system to load the file from.</param>
        /// <param name="path">The file to load from the file system.</param>
        /// <param name="parentFileState">The state from which the file system originates.</param>
        /// <param name="loadFileContext">The context with additional parameters for the load process.</param>
        /// <returns>The loaded <see cref="IFileState"/> for the file.</returns>
        Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, IFileState parentFileState, LoadFileContext loadFileContext);

        #endregion

        #endregion

        /// <summary>
        /// Save a loaded state to a physical path.
        /// </summary>
        /// <param name="fileState">The <see cref="IFileState"/> to save.</param>
        /// <param name="saveFile">The physical path at which to save the file.</param>
        /// <returns></returns>
        Task<SaveResult> SaveFile(IFileState fileState, string saveFile);
    }
}
