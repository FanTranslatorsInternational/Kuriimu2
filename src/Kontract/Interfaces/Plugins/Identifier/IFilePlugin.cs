using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace Kontract.Interfaces.Plugins.Identifier
{
    /// <summary>
    /// Base interface for plugins that handle files.
    /// </summary>
    /// <see cref="PluginType"/> for the supported types of files.
    public interface IFilePlugin : IPlugin
    {
        /// <summary>
        /// The type of file the plugin can handle.
        /// </summary>
        PluginType PluginType { get; }
        
        /// <summary>
        /// All file extensions the format can be identified with.
        /// </summary>
        string[] FileExtensions { get; }

        /// <summary>
        /// Creates an <see cref="IPluginState"/> to further work with the file.
        /// </summary>
        /// <param name="pluginManager">The plugin manager to load files with the Kuriimu runtime.</param>
        /// <returns>Newly created <see cref="IPluginState"/>.</returns>
        IPluginState CreatePluginState(IPluginManager pluginManager);
    }
}
