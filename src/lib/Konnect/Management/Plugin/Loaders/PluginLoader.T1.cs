using Konnect.Contract.DataClasses.Management.Plugin.Loaders;
using Konnect.Contract.Management.Plugin.Loaders;
using System.Reflection;
using Konnect.Contract.Plugin;

namespace Konnect.Management.Plugin.Loaders
{
    public class PluginLoader<TPlugin> : PluginLoader, IPluginLoader<TPlugin>
        where TPlugin : IPlugin
    {
        /// <inheritdoc />
        public override IReadOnlyList<PluginLoadError> LoadErrors { get; }

        /// <inheritdoc />
        public IReadOnlyList<TPlugin> Plugins { get; }

        public PluginLoader(params string[] pluginPaths)
        {
            if (!TryLoadPlugins<TPlugin>(pluginPaths, out var plugins, out var errors))
            {
                errors = new List<PluginLoadError>();
                plugins = new List<TPlugin>();
            }

            LoadErrors = errors;
            Plugins = plugins;
        }

        public PluginLoader(params Assembly[] pluginAssemblies)
        {
            if (!TryLoadPlugins<TPlugin>(pluginAssemblies, out var plugins, out var errors))
            {
                errors = new List<PluginLoadError>();
                plugins = new List<TPlugin>();
            }

            LoadErrors = errors;
            Plugins = plugins;
        }

        /// <inheritdoc />
        public override bool Exists(Guid pluginId)
        {
            return Plugins.Any(p => p.PluginId == pluginId);
        }

        /// <inheritdoc />
        public TPlugin? GetPlugin(Guid pluginId)
        {
            return Plugins.FirstOrDefault(ep => ep.PluginId == pluginId);
        }
    }
}
