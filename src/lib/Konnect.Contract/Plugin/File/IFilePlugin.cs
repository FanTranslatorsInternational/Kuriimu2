using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;

namespace Konnect.Contract.Plugin.File;

/// <summary>
/// Base interface for plugins that handle files.
/// </summary>
/// <see cref="PluginType"/> for the supported types of files.
public interface IFilePlugin : IPlugin
{
    /// <summary>
    /// The type of file the plugin can handle.
    /// </summary>
    PluginType PluginType { get; }

    /// <summary>
    /// All file extensions the format can be identified with.
    /// </summary>
    string[] FileExtensions { get; }

    /// <summary>
    /// Creates an <see cref="IFilePluginState"/> to further work with the file.
    /// </summary>
    /// <param name="pluginFileManager">The plugin manager to load files with the Kuriimu runtime.</param>
    /// <returns>Newly created <see cref="IFilePluginState"/>.</returns>
    IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager);

    #region Optional feature support checks

    bool CanIdentifyFiles => this is IIdentifyFiles;

    bool IsDeprecated => this is IDeprecatedFilePlugin;

    #endregion

    #region Optional feature casting defaults

    Task<bool> AttemptIdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext) =>
        (this as IIdentifyFiles)?.IdentifyAsync(fileSystem, filePath, identifyContext) ?? Task.FromResult(false);

    IDeprecatedFilePlugin? Deprecated => this as IDeprecatedFilePlugin;

    #endregion
}