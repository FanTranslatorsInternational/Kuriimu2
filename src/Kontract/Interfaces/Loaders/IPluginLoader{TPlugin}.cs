using System;
using System.Collections.Generic;

namespace Kontract.Interfaces.Loaders
{
    /// <summary>
    /// Exposes methods for methods with generic plugin types.
    /// </summary>
    /// <typeparam name="TPlugin">The type of the plugin to retrieve.</typeparam>
    public interface IPluginLoader<out TPlugin> : IPluginLoader
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
