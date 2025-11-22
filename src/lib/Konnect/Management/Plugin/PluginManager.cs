using Konnect.Contract.DataClasses.Management.Plugin.Loaders;
using Konnect.Contract.Management.Plugin;
using Konnect.Contract.Management.Plugin.Loaders;
using Konnect.Contract.Plugin;

namespace Konnect.Management.Plugin;

public class PluginManager : IPluginManager
{
    private readonly IPluginLoader[] _pluginLoaders;

    public PluginManager(params IPluginLoader[] pluginLoaders)
    {
        _pluginLoaders = pluginLoaders;
    }

    public IReadOnlyList<PluginLoadError> GetErrors()
    {
        return _pluginLoaders.SelectMany(pl => pl.LoadErrors).DistinctBy(e => e.AssemblyPath).ToArray();
    }

    public IEnumerable<TPlugin> GetPlugins<TPlugin>() where TPlugin : IPlugin
    {
        return _pluginLoaders.Where(x => x is IPluginLoader<TPlugin>).Cast<IPluginLoader<TPlugin>>().SelectMany(l => l.Plugins);
    }

    public TPlugin? GetPlugin<TPlugin>(Guid pluginId) where TPlugin : IPlugin
    {
        return GetPlugins<TPlugin>().FirstOrDefault(p => p.PluginId == pluginId);
    }
}