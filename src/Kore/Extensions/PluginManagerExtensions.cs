using System.Collections.Generic;
using System.Linq;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State.Game;
using Kore.Managers.Plugins;

namespace Kore.Extensions
{
    public static class PluginManagerExtensions
    {
        public static IEnumerable<IFilePlugin> GetFilePlugins(this IInternalPluginManager pluginManager)
        {
            return pluginManager.GetFilePluginLoaders().SelectMany(x => x.Plugins);
        }

        public static IEnumerable<IFilePlugin> GetIdentifiableFilePlugins(this IInternalPluginManager pluginManager)
        {
            return pluginManager.GetFilePluginLoaders().GetIdentifiableFilePlugins();
        }

        public static IEnumerable<IFilePlugin> GetNonIdentifiableFilePlugins(this IInternalPluginManager pluginManager)
        {
            return pluginManager.GetFilePluginLoaders().GetNonIdentifiableFilePlugins();
        }

        public static IEnumerable<IGameAdapter> GetGameAdapters(this IInternalPluginManager pluginManager)
        {
            return pluginManager.GetGamePluginLoaders().SelectMany(x => x.Plugins);
        }
    }
}
