using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kontract.Interfaces.Plugins.Entry;
using Kontract.Interfaces.Plugins.Loaders;
using Kontract.Models.Plugins.Loaders;

namespace Kore.Managers.Plugins.PluginLoader
{
    public class AssemblyFilePluginLoader : CsPluginLoader, IPluginLoader<IFilePlugin>
    {
        /// <inheritdoc />
        public IReadOnlyList<PluginLoadError> LoadErrors { get; }

        /// <inheritdoc />
        public IReadOnlyList<IFilePlugin> Plugins { get; private set; }

        public AssemblyFilePluginLoader(params Assembly[] pluginAssemblies)
        {
            if (!TryLoadPlugins<IFilePlugin>(pluginAssemblies, out var plugins, out var errors))
                LoadErrors = errors;

            Plugins = plugins;
        }

        /// <inheritdoc />
        public bool Exists(Guid pluginId)
        {
            return Plugins.Any(p => p.PluginId == pluginId);
        }

        /// <inheritdoc />
        public IFilePlugin GetPlugin(Guid pluginId)
        {
            return Plugins.FirstOrDefault(ep => ep.PluginId == pluginId);
        }
    }
}
