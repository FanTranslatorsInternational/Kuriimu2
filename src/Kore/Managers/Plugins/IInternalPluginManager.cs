using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kontract.Interfaces.Loaders;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State.Game;
using Kontract.Interfaces.Progress;
using Kontract.Models;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace Kore.Managers.Plugins
{
    /// <summary>
    /// Exposes methods to load physical and virtual files directly.
    /// </summary>
    public interface IInternalPluginManager : IPluginManager
    {
        /// <summary>
        /// The errors the plugins produced when loaded.
        /// </summary>
        IReadOnlyList<PluginLoadError> LoadErrors { get; }

        /// <summary>
        /// Determines if a file is already loaded.
        /// </summary>
        /// <param name="filePath">The path of the file to check.</param>
        /// <returns>If the file is already loaded.</returns>
        bool IsLoaded(UPath filePath);

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

        /// <summary>
        /// Loads a physical path into the Kuriimu runtime.
        /// </summary>
        /// <param name="file">The path to the path to load.</param>
        /// <param name="progress">The context to report progress.</param>
        /// <returns>The loaded state of the path.</returns>
        Task<LoadResult> LoadFile(string file, IProgressContext progress = null);

        /// <summary>
        /// Loads a physical path into the Kuriimu runtime.
        /// </summary>
        /// <param name="file">The path to the path to load.</param>
        /// <param name="pluginId">the plugin with which to load the file.</param>
        /// <param name="progress">The context to report progress.</param>
        /// <returns>The loaded state of the path.</returns>
        Task<LoadResult> LoadFile(string file, Guid pluginId, IProgressContext progress = null);

        /// <summary>
        /// Loads a virtual path into the Kuriimu runtime.
        /// </summary>
        /// <param name="stateInfo">The loaded path state to load a path from.</param>
        /// <param name="afi">The path to load from that state.</param>
        /// <param name="progress">The context to report progress.</param>
        /// <returns>The loaded state of the path.</returns>
        Task<LoadResult> LoadFile(IStateInfo stateInfo, ArchiveFileInfo afi, IProgressContext progress = null);

        /// <summary>
        /// Loads a virtual path into the Kuriimu runtime.
        /// </summary>
        /// <param name="stateInfo">The loaded path state to load a path from.</param>
        /// <param name="afi">The path to load from that state.</param>
        /// <param name="pluginId">The plugin to load this virtual file with.</param>
        /// <param name="progress">The context to report progress.</param>
        /// <returns>The loaded state of the path.</returns>
        Task<LoadResult> LoadFile(IStateInfo stateInfo, ArchiveFileInfo afi, Guid pluginId, IProgressContext progress = null);

        /// <summary>
        /// Save a loaded state to the given path.
        /// </summary>
        /// <param name="stateInfo">The <see cref="IStateInfo"/> to save.</param>
        /// <param name="saveName">The path at which to save the file.</param>
        /// <returns></returns>
        Task<SaveResult> SaveFile(IStateInfo stateInfo, UPath saveName);

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
