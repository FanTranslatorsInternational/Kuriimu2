using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File.Text;
using Konnect.Contract.Management.Files;

namespace Konnect.Contract.Plugin.Game;

/// <summary>
/// Interface for plugins that handle game-specific behaviours.
/// </summary>
public interface IGamePlugin : IPlugin
{
    /// <summary>
    /// Creates an <see cref="IGamePluginState"/> to further work with the file.
    /// </summary>
    /// <param name="filePath">The relative path of the text file to identify the type of preview.</param>
    /// <param name="entries">The text entries to identify the type of preview.</param>
    /// <param name="pluginFileManager">The plugin manager to load files with the Kuriimu runtime.</param>
    /// <returns>Newly created <see cref="IGamePluginState"/>.</returns>
    IGamePluginState CreatePluginState(UPath filePath, IReadOnlyList<TextEntry> entries, IPluginFileManager pluginFileManager);
}