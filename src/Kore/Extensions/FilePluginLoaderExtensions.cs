using System.Collections.Generic;
using System.Linq;
using Kontract.Interfaces.Loaders;
using Kontract.Interfaces.Plugins.Identifier;

namespace Kore.Extensions
{
    public static class FilePluginLoaderExtensions
    {
        public static IEnumerable<IIdentifyFiles> GetIdentifiableFilePlugins(this IEnumerable<IPluginLoader<IFilePlugin>> filePluginLoaders)
        {
            return filePluginLoaders.SelectMany(x => x.Plugins).Where(x => x is IIdentifyFiles).Cast<IIdentifyFiles>();
        }

        public static IEnumerable<IFilePlugin> GetNonIdentifiableFilePlugins(this IEnumerable<IPluginLoader<IFilePlugin>> filePluginLoaders)
        {
            return filePluginLoaders.SelectMany(x => x.Plugins).Where(x => !(x is IIdentifyFiles));
        }
    }
}
