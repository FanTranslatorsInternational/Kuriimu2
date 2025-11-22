using Konnect.Contract.DataClasses.Management.Plugin.Loaders;

namespace Konnect.Contract.Management.Plugin.Loaders;

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