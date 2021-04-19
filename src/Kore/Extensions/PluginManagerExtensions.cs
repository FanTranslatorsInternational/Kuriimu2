using System.Collections.Generic;
using System.Linq;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State.Game;
using Kore.Managers.Plugins;

namespace Kore.Extensions
{
    public static class PluginManagerExtensions
    {
        public static IEnumerable<IFilePlugin> GetFilePlugins(this IInternalFileManager fileManager)
        {
            return fileManager.GetFilePluginLoaders().SelectMany(x => x.Plugins);
        }

        public static IEnumerable<IFilePlugin> GetIdentifiableFilePlugins(this IInternalFileManager fileManager)
        {
            return fileManager.GetFilePluginLoaders().GetIdentifiableFilePlugins();
        }

        public static IEnumerable<IFilePlugin> GetNonIdentifiableFilePlugins(this IInternalFileManager fileManager)
        {
            return fileManager.GetFilePluginLoaders().GetNonIdentifiableFilePlugins();
        }

        public static IEnumerable<IGameAdapter> GetGameAdapters(this IInternalFileManager fileManager)
        {
            return fileManager.GetGamePluginLoaders().SelectMany(x => x.Plugins);
        }
    }
}
