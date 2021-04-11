using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Loaders;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State.Game;
using Kontract.Models;
using Kontract.Models.Context;
using Kontract.Models.IO;
using Serilog;

namespace Kore.Managers.Plugins
{
    /// <summary>
    /// Exposes methods to load physical and virtual files directly.
    /// </summary>
    public interface IInternalPluginManager : IPluginManager, IDisposable
    {
        /// <summary>
        /// An event to allow for manual selection by the user.
        /// </summary>
        event EventHandler<ManualSelectionEventArgs> OnManualSelection;

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
        /// Gets the <see cref="IStateInfo"/> of the requested file.
        /// </summary>
        /// <param name="filePath">The path of the file to request.</param>
        /// <returns>The <see cref="IStateInfo"/> of the file.</returns>
        IStateInfo GetLoadedFile(UPath filePath);

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
        /// <param name="parentStateInfo">The state from which the file system originates.</param>
        /// <returns>The loaded <see cref="IStateInfo"/> for the file.</returns>
        Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, IStateInfo parentStateInfo);

        /// <summary>
        /// Loads a file from a given file system.
        /// </summary>
        /// <param name="fileSystem">The file system to load the file from.</param>
        /// <param name="path">The file to load from the file system.</param>
        /// <param name="pluginId">The Id of the plugin to load the file with.</param>
        /// <param name="parentStateInfo">The state from which the file system originates.</param>
        /// <returns>The loaded <see cref="IStateInfo"/> for the file.</returns>
        Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, Guid pluginId, IStateInfo parentStateInfo);

        /// <summary>
        /// Loads a file from a given file system.
        /// </summary>
        /// <param name="fileSystem">The file system to load the file from.</param>
        /// <param name="path">The file to load from the file system.</param>
        /// <param name="parentStateInfo">The state from which the file system originates.</param>
        /// <param name="loadFileContext">The context with additional parameters for the load process.</param>
        /// <returns>The loaded <see cref="IStateInfo"/> for the file.</returns>
        Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, IStateInfo parentStateInfo, LoadFileContext loadFileContext);

        #endregion

        #endregion

        /// <summary>
        /// Save a loaded state to a physical path.
        /// </summary>
        /// <param name="stateInfo">The <see cref="IStateInfo"/> to save.</param>
        /// <param name="saveFile">The physical path at which to save the file.</param>
        /// <returns></returns>
        Task<SaveResult> SaveFile(IStateInfo stateInfo, string saveFile);
    }
}
