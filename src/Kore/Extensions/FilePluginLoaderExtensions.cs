using System.Collections.Generic;
using System.Linq;
using Kontract.Interfaces.Loaders;
using Kontract.Interfaces.Plugins.Identifier;

namespace Kore.Extensions
{
    /// <summary>
    /// Offers methods to extend on <see cref="IPluginLoader{IFilePlugin}"/>.
    /// </summary>
    public static class FilePluginLoaderExtensions
    {
        /// <summary>
        /// Enumerate all plugins that implement <see cref="IIdentifyFiles"/>.
        /// </summary>
        /// <returns>All identifiable plugins.</returns>
        public static IEnumerable<IIdentifyFiles> GetIdentifiablePlugins(this IPluginLoader<IFilePlugin> filePluginLoader)
        {
            return filePluginLoader.Plugins.Where(ep => ep is IIdentifyFiles).Cast<IIdentifyFiles>();
        }

        /// <summary>
        /// Enumerate all plugins that don't implement <see cref="IIdentifyFiles"/>.
        /// </summary>
        /// <returns>All non-identifiable plugins.</returns>
        public static IEnumerable<IFilePlugin> GetNonIdentifiablePlugins(this IPluginLoader<IFilePlugin> filePluginLoader)
        {
            return filePluginLoader.Plugins.Where(ep => !(ep is IIdentifyFiles));
        }
    }
}
