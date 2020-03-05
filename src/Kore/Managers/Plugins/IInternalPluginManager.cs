using System;
using System.Threading.Tasks;
using Kontract.Interfaces.Loaders;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State.Game;
using Kontract.Interfaces.Progress;
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
        /// Retrieves all <see cref="IPluginLoader"/>s that can load files.
        /// </summary>
        /// <returns></returns>
        IPluginLoader<IFilePlugin>[] GetFilePluginLoaders();

        /// <summary>
        /// Retrieves all <see cref="IPluginLoader"/>s that can render game previews.
        /// </summary>
        /// <returns></returns>
        IPluginLoader<IGameAdapter>[] GetGamePluginLoaders();

        bool IsLoaded(UPath filePath);

        IStateInfo GetLoadedFile(UPath filePath);

        /// <summary>
        /// Loads a physical path into the Kuriimu runtime.
        /// </summary>
        /// <param name="file">The path to the path to load.</param>
        /// <param name="progress">The context to report progress.</param>
        /// <returns>The loaded state of the path.</returns>
        Task<IStateInfo> LoadFile(string file, IProgressContext progress = null);

        Task<IStateInfo> LoadFile(string file, Guid pluginId, IProgressContext progress = null);

        /// <summary>
        /// Loads a virtual path into the Kuriimu runtime.
        /// </summary>
        /// <param name="stateInfo">The loaded path state to load a path from.</param>
        /// <param name="afi">The path to load from that state.</param>
        /// <param name="progress">The context to report progress.</param>
        /// <returns>The loaded state of the path.</returns>
        Task<IStateInfo> LoadFile(IStateInfo stateInfo, ArchiveFileInfo afi, IProgressContext progress = null);

        /// <summary>
        /// Loads a virtual path into the Kuriimu runtime.
        /// </summary>
        /// <param name="stateInfo">The loaded path state to load a path from.</param>
        /// <param name="afi">The path to load from that state.</param>
        /// <param name="pluginId">The plugin to load this virtual file with.</param>
        /// <param name="progress">The context to report progress.</param>
        /// <returns>The loaded state of the path.</returns>
        Task<IStateInfo> LoadFile(IStateInfo stateInfo, ArchiveFileInfo afi, Guid pluginId, IProgressContext progress = null);

        Task SaveFile(IStateInfo stateInfo, UPath saveName);

        void Close(IStateInfo stateInfo);

        void CloseAll();
    }
}
