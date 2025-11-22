using Konnect.Contract.DataClasses.Management.Plugin.Loaders;
using Konnect.Contract.Plugin;

namespace Konnect.Contract.Management.Plugin;

public interface IPluginManager
{
    IReadOnlyList<PluginLoadError> GetErrors();
    IEnumerable<TPlugin> GetPlugins<TPlugin>() where TPlugin : IPlugin;
    TPlugin? GetPlugin<TPlugin>(Guid pluginId) where TPlugin : IPlugin;
}