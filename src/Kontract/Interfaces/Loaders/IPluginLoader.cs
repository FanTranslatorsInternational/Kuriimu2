using System;

namespace Kontract.Interfaces.Loaders
{
    /// <summary>
    /// Exposes methods to load and get plugins.
    /// </summary>
    public interface IPluginLoader
    {
        /// <summary>
        /// Checks if a plugin exists in this instance.
        /// </summary>
        /// <param name="pluginId">The unique Id of a plugin.</param>
        /// <returns>If the plugin exists in this instance.</returns>
        bool Exists(Guid pluginId);
    }
}
