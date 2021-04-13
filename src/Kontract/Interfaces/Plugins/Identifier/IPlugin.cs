using System;
using Kontract.Models;

namespace Kontract.Interfaces.Plugins.Identifier
{
    /// <summary>
    /// Base interface that each plugin has to derive from.
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// A static and unique Id.
        /// </summary>
        /// <remarks>If more plugins have the same Id, the first plugin loaded by the CLR will be prioritized.</remarks>
        Guid PluginId { get; }

        /// <summary>
        /// The metadata for this plugin.
        /// </summary>
        PluginMetadata Metadata { get; }
    }
}