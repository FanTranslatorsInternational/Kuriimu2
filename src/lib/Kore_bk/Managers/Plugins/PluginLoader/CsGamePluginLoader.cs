using System;
using System.Collections.Generic;
using System.Linq;
using Kontract.Interfaces.Plugins.Loaders;
using Kontract.Models.Plugins.Loaders;

namespace Kore.Managers.Plugins.PluginLoader
{
    class CsGamePluginLoader : CsPluginLoader, IPluginLoader<IGameAdapter>
    {
        /// <inheritdoc />
        public IReadOnlyList<PluginLoadError> LoadErrors { get; }

        /// <inheritdoc />
        public IReadOnlyList<IGameAdapter> Plugins { get; private set; }

        public CsGamePluginLoader(params string[] pluginPaths)
        {
            if (!TryLoadPlugins<IGameAdapter>(pluginPaths, out var plugins, out var errors))
                LoadErrors = errors;

            Plugins = plugins;
        }

        /// <inheritdoc />
        public bool Exists(Guid pluginId)
        {
            return Plugins.Any(p => p.PluginId == pluginId);
        }

        /// <inheritdoc />
        public IGameAdapter GetPlugin(Guid pluginId)
        {
            return Plugins.FirstOrDefault(ep => ep.PluginId == pluginId);
        }
    }
}
