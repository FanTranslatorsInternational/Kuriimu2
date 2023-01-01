using System.Collections.Generic;
using System.Linq;
using Kontract.Interfaces.Plugins.Entry;
using Kore.Managers.Plugins;

namespace Kore.Extensions
{
    public static class PluginManagerExtensions
    {
        public static IEnumerable<IFilePlugin> GetFilePlugins(this IFileManager fileManager)
        {
            return fileManager.GetFilePluginLoaders().SelectMany(x => x.Plugins);
        }

        public static IEnumerable<IIdentifyFiles> GetIdentifiableFilePlugins(this IFileManager fileManager)
        {
            return fileManager.GetFilePluginLoaders().GetIdentifiableFilePlugins();
        }

        public static IEnumerable<IFilePlugin> GetNonIdentifiableFilePlugins(this IFileManager fileManager)
        {
            return fileManager.GetFilePluginLoaders().GetNonIdentifiableFilePlugins();
        }

        public static IEnumerable<IGameAdapter> GetGameAdapters(this IFileManager fileManager)
        {
            return fileManager.GetGamePluginLoaders().SelectMany(x => x.Plugins);
        }
    }
}
