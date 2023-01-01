using System.Collections.Generic;
using System.Linq;
using Kontract.Interfaces.Plugins.Entry;
using Kontract.Interfaces.Plugins.Loaders;

namespace Kore.Extensions
{
    public static class FilePluginLoaderExtensions
    {
        public static IEnumerable<IFilePlugin> GetAllFilePlugins(this IEnumerable<IPluginLoader<IFilePlugin>> filePluginLoaders)
        {
            return filePluginLoaders.SelectMany(x => x.Plugins);
        }
        
        public static IEnumerable<IIdentifyFiles> GetIdentifiableFilePlugins(this IEnumerable<IPluginLoader<IFilePlugin>> filePluginLoaders)
        {
            return filePluginLoaders.SelectMany(x => x.Plugins).Where(x => x.CanIdentifyFiles).Cast<IIdentifyFiles>();
        }

        public static IEnumerable<IFilePlugin> GetNonIdentifiableFilePlugins(this IEnumerable<IPluginLoader<IFilePlugin>> filePluginLoaders)
        {
            return filePluginLoaders.SelectMany(x => x.Plugins).Where(x => !x.CanIdentifyFiles);
        }
    }
}
