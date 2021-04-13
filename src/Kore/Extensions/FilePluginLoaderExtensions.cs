using System.Collections.Generic;
using System.Linq;
using Kontract.Extensions;
using Kontract.Interfaces.Loaders;
using Kontract.Interfaces.Plugins.Identifier;

namespace Kore.Extensions
{
    public static class FilePluginLoaderExtensions
    {
        public static IEnumerable<IFilePlugin> GetAllFilePlugins(this IEnumerable<IPluginLoader<IFilePlugin>> filePluginLoaders)
        {
            return filePluginLoaders.SelectMany(x => x.Plugins);
        }
        
        public static IEnumerable<IFilePlugin> GetIdentifiableFilePlugins(this IEnumerable<IPluginLoader<IFilePlugin>> filePluginLoaders)
        {
            return filePluginLoaders.SelectMany(x => x.Plugins).Where(x => x.CanIdentifyFiles);
        }

        public static IEnumerable<IFilePlugin> GetNonIdentifiableFilePlugins(this IEnumerable<IPluginLoader<IFilePlugin>> filePluginLoaders)
        {
            return filePluginLoaders.SelectMany(x => x.Plugins).Where(x => !x.CanIdentifyFiles);
        }
    }
}
