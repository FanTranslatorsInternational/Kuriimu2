using System;
using System.Collections.Generic;
using System.Linq;
using Kontract.Interfaces.Loaders;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Models;

namespace Kore.Managers.Plugins.PluginLoader
{
    class CsFilePluginLoader : CsPluginLoader, IPluginLoader<IFilePlugin>
    {
        /// <inheritdoc />
        public IReadOnlyList<PluginLoadError> LoadErrors { get; }

        /// <inheritdoc />
        public IReadOnlyList<IFilePlugin> Plugins { get; private set; }

        public CsFilePluginLoader(params string[] pluginPaths)
        {
            if (!TryLoadPlugins<IFilePlugin>(pluginPaths, out var plugins, out var errors))
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
