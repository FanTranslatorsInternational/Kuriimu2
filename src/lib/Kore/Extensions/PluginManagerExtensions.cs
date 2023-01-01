using System.Collections.Generic;
using System.Linq;
using Kontract.Interfaces.Managers.Files;
using Kontract.Interfaces.Plugins.Entry;
using Kore.Managers.Plugins;

namespace Kore.Extensions
{
    public static class PluginManagerExtensions
    {
        public static IEnumerable<IFilePlugin> GetFilePlugins(this IKoreFileManager fileManager)
        {
            return fileManager.GetFilePluginLoaders().SelectMany(x => x.Plugins);
        }

        public static IEnumerable<IIdentifyFiles> GetIdentifiableFilePlugins(this IKoreFileManager fileManager)
        {
            return fileManager.GetFilePluginLoaders().GetIdentifiableFilePlugins();
        }

        public static IEnumerable<IFilePlugin> GetNonIdentifiableFilePlugins(this IKoreFileManager fileManager)
        {
            return fileManager.GetFilePluginLoaders().GetNonIdentifiableFilePlugins();
        }

        //public static IEnumerable<IGameAdapter> GetGameAdapters(this IFileManager fileManager)
        //{
        //    return fileManager.GetGamePluginLoaders().SelectMany(x => x.Plugins);
        //}
    }
}
