using Konnect.Contract.DataClasses.Management.Plugin.Loaders;
using Konnect.Contract.Management.Plugin;
using Konnect.Contract.Management.Plugin.Loaders;
using Konnect.Contract.Plugin;

namespace Konnect.Management.Plugin;

public class PluginManager(params IPluginLoader[] pluginLoaders) : IPluginManager
{
    public IReadOnlyList<PluginLoadError> GetErrors()
    {
        return [.. pluginLoaders.SelectMany(pl => pl.LoadErrors).DistinctBy(e => e.AssemblyPath)];
    }

    public IEnumerable<TPlugin> GetPlugins<TPlugin>() where TPlugin : IPlugin
    {
        return pluginLoaders.OfType<IPluginLoader<TPlugin>>().SelectMany(l => l.Plugins);
    }

    public TPlugin? GetPlugin<TPlugin>(Guid pluginId) where TPlugin : IPlugin
    {
        return GetPlugins<TPlugin>().FirstOrDefault(p => p.PluginId == pluginId);
    }
}