using System;
using System.Collections.Generic;
using Kontract.Models;

namespace Kontract.Interfaces.Loaders
{
    /// <summary>
    /// Exposes methods to load and get plugins.
    /// </summary>
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
}
