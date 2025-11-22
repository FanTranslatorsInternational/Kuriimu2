using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;

namespace Konnect.Contract.Plugin.File;

/// <summary>
/// Base interface for deprecated plugins that handle files.
/// </summary>
/// <see cref="PluginType"/> for the supported types of files.
public interface IDeprecatedFilePlugin : IFilePlugin
{
    /// <summary>
    /// Determines a list of alternative 3rd-party tools that could open this file format.
    /// </summary>
    DeprecatedPluginAlternative[] Alternatives { get; }
}