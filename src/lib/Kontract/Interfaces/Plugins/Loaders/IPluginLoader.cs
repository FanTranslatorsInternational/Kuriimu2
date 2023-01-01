using System;
using System.Collections.Generic;
using Kontract.Interfaces.Plugins.Entry;
using Kontract.Models.Plugins.Loaders;

namespace Kontract.Interfaces.Plugins.Loaders
{
    /// <summary>
    /// Exposes methods to retrieve non-generic information about loaded plugins.
    /// This interface is only intended as a base marker interface, please inherit from the generic version.
    /// </summary>
    /// <see cref="IPluginLoader{TPlugin}"/>
    public interface IPluginLoader
    {
        /// <summary>
        /// A read-only list of errors when loading plugins.
        /// </summary>
        IReadOnlyList<PluginLoadError> LoadErrors { get; }

        /// <summary>
        /// Checks if a plugin exists in this instance.
        /// </summary>
        /// <param name="pluginId">The unique Id of a plugin.</param>
        /// <returns>If the plugin exists in this instance.</returns>
        bool Exists(Guid pluginId);
    }

    /// <summary>
    /// Exposes methods to handle a specific type of plugin.
    /// </summary>
    /// <typeparam name="TPlugin">The type of the plugin to retrieve.</typeparam>
    public interface IPluginLoader<out TPlugin> : IPluginLoader where TPlugin : IPlugin
    {
        /// <summary>
        /// A read-only list of plugins loaded by this instance.
        /// </summary>
        IReadOnlyList<TPlugin> Plugins { get; }

        /// <summary>
        /// Gets a plugin from this instance.
        /// </summary>
        /// <param name="pluginId">The unique Id of a plugin.</param>
        /// <returns>The plugin with the given unique Id.</returns>
        TPlugin GetPlugin(Guid pluginId);
    }
}
