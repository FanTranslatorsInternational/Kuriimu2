using System.Threading.Tasks;
using Kontract.Interfaces;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Progress;
using Kore.Models.LoadInfo;

namespace Kore.Managers.Plugins.FileManagement
{
    /// <summary>
    /// Exposes methods to load files into a plugin state.
    /// </summary>
    internal interface IFileLoader
    {
        /// <summary>
        /// Loads a physical file into a plugin state.
        /// </summary>
        /// <param name="loadInfo">The context to hold the information to the physical file.</param>
        /// <param name="pluginManager">The plugin manager to load files with the Kuriimu runtime.</param>
        /// <param name="progress">The context to report progress.</param>
        /// <returns>The loaded state of the file.</returns>
        Task<IStateInfo> LoadAsync(PhysicalLoadInfo loadInfo, IPluginManager pluginManager, IProgressContext progress = null);

        /// <summary>
        /// Loads a virtual file (eg an ArchiveFileInfo) into a plugin state.
        /// </summary>
        /// <param name="loadInfo">The context to hold the information to the virtual file.</param>
        /// <param name="pluginManager">The plugin manager to load files with the Kuriimu runtime.</param>
        /// <param name="progress">The context to report progress.</param>
        /// <returns>The loaded state of the file.</returns>
        Task<IStateInfo> LoadAsync(VirtualLoadInfo loadInfo, IPluginManager pluginManager, IProgressContext progress = null);

        /// <summary>
        /// Loads any file into a plugin state from within another plugin.
        /// </summary>
        /// <param name="loadInfo">The context to hold the information to the file.</param>
        /// <param name="pluginManager">The plugin manager to load files with the Kuriimu runtime.</param>
        /// <param name="progress">The context to report progress.</param>
        /// <returns>The loaded state of the file.</returns>
        Task<IStateInfo> LoadAsync(PluginLoadInfo loadInfo, IPluginManager pluginManager, IProgressContext progress = null);
    }
}
