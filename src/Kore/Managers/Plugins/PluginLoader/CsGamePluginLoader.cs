using System;
using System.Collections.Generic;
using System.Linq;
using Kontract.Interfaces.Loaders;
using Kontract.Interfaces.Plugins.State.Game;

namespace Kore.Managers.Plugins.PluginLoader
{
    class CsGamePluginLoader : CsPluginLoader, IPluginLoader<IGameAdapter>
    {
        /// <inheritdoc />
        public IReadOnlyList<IGameAdapter> Plugins { get; private set; }

        public CsGamePluginLoader(params string[] pluginPaths)
        {
            if (!TryLoadPlugins<IGameAdapter>(pluginPaths, out var plugins, out var errors))
            {
                throw new AggregateException(errors.Select(e => new InvalidOperationException(e.ToString())));
            }

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
