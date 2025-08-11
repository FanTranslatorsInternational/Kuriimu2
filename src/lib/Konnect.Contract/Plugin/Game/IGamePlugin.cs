using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace Konnect.Contract.Plugin.Game
{
    /// <summary>
    /// Interface for plugins that handle previews.
    /// </summary>
    public interface IGamePlugin : IPlugin
    {
        /// <summary>
        /// Creates an <see cref="IFilePluginState"/> to further work with the file.
        /// </summary>
        /// <param name="pluginFileManager">The plugin manager to load files with the Kuriimu runtime.</param>
        /// <returns>Newly created <see cref="IFilePluginState"/>.</returns>
        IGamePluginState CreatePluginState(IPluginFileManager pluginFileManager);
    }
}
