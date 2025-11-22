namespace Konnect.Contract.DataClasses.Plugin;

/// <summary>
/// Describes a 3rd-party application for deprecated plugins.
/// </summary>
public class DeprecatedPluginAlternative
{
    /// <summary>
    /// The name of the 3rd-party tool.
    /// </summary>
    public required string ToolName { get; init; }

    /// <summary>
    /// The url to the 3rd-party tool.
    /// </summary>
    public required string Url { get; init; }
}