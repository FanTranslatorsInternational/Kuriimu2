using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace Kontract.Interfaces.Plugins.Identifier
{
    /// <summary>
    /// Base interface that each plugin has to derive from.
    /// </summary>
    public interface IFilePlugin
    {
        /// <summary>
        /// A static and unique Id.
        /// </summary>
        /// <remarks>If more plugins have the same Id, the first plugin loaded by the CLR will be prioritized.</remarks>
        Guid PluginId { get; }

        /// <summary>
        /// The type of file the plugin can handle.
        /// </summary>
        PluginType PluginType { get; }

        /// <summary>
        /// All file extensions the format can be identified with.
        /// </summary>
        string[] FileExtensions { get; }

        /// <summary>
        /// The metadata for this plugin.
        /// </summary>
        PluginMetadata Metadata { get; }

        /// <summary>
        /// Creates an <see cref="IPluginState"/> to further work with the file.
        /// </summary>
        /// <param name="pluginManager">The plugin manager to load files with the Kuriimu runtime.</param>
        /// <returns>Newly created <see cref="IPluginState"/>.</returns>
        IPluginState CreatePluginState(IPluginManager pluginManager);
    }
}
