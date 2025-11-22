using Konnect.Contract.Plugin;

namespace Konnect.Contract.Management.Plugin.Loaders;

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
    TPlugin? GetPlugin(Guid pluginId);
}