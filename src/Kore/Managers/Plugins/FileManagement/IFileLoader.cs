using System;
using System.Threading.Tasks;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Progress;
using Kontract.Models;
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
        event EventHandler<ManualSelectionEventArgs> OnManualSelection;

        /// <summary>
        /// Loads a physical file into a plugin state.
        /// </summary>
        /// <param name="loadInfo">The context to hold the information to the physical file.</param>
        /// <param name="pluginManager">The plugin manager to load files with the Kuriimu runtime.</param>
        /// <param name="loadPluginManually">Declares if plugins can be selected by the user, if not automatically identified.</param>
        /// <param name="progress">The context to report progress.</param>
        /// <returns>The loaded state of the file.</returns>
        Task<LoadResult> LoadAsync(PhysicalLoadInfo loadInfo, IPluginManager pluginManager,
            bool loadPluginManually, IProgressContext progress = null);

        /// <summary>
        /// Loads a virtual file (eg an ArchiveFileInfo) into a plugin state.
        /// </summary>
        /// <param name="loadInfo">The context to hold the information to the virtual file.</param>
        /// <param name="pluginManager">The plugin manager to load files with the Kuriimu runtime.</param>
        /// <param name="loadPluginManually">Declares if plugins can be selected by the user, if not automatically identified.</param>
        /// <param name="progress">The context to report progress.</param>
        /// <returns>The loaded state of the file.</returns>
        Task<LoadResult> LoadAsync(VirtualLoadInfo loadInfo, IPluginManager pluginManager,
            bool loadPluginManually, IProgressContext progress = null);

        /// <summary>
        /// Loads any file into a plugin state from within another plugin.
        /// </summary>
        /// <param name="loadInfo">The context to hold the information to the file.</param>
        /// <param name="pluginManager">The plugin manager to load files with the Kuriimu runtime.</param>
        /// <param name="loadPluginManually">Declares if plugins can be selected by the user, if not automatically identified.</param>
        /// <param name="progress">The context to report progress.</param>
        /// <returns>The loaded state of the file.</returns>
        Task<LoadResult> LoadAsync(PluginLoadInfo loadInfo, IPluginManager pluginManager,
            bool loadPluginManually, IProgressContext progress = null);
    }
}
