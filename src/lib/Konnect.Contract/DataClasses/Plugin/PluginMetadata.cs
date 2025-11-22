namespace Konnect.Contract.DataClasses.Plugin;

/// <summary>
/// Offers additional information to the plugin.
/// </summary>
public sealed class PluginMetadata
{
    /// <summary>
    /// The name of the plugin or its supported format(s).
    /// Often its file magic.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The author/developer of this plugin.
    /// Any possible notation the author is known as can be used.
    /// </summary>
    public required string[] Author { get; init; }

    /// <summary>
    /// The platform the format was encountered on.
    /// </summary>
    public required string[] Platform { get; init; }

    /// <summary>
    /// The developer of the game or format.
    /// </summary>
    public required string Developer { get; init; }

    /// <summary>
    /// The publisher of the game a format is from.
    /// Only necessary if the format is distinctly used by a single game or in combination of the same developer-publisher pair.
    /// </summary>
    public string? Publisher { get; init; }

    /// <summary>
    /// The long form description of the plugin.
    /// "A Kuriimu2 plugin to support a certain file format with meta information."
    /// </summary>
    public string? LongDescription { get; init; }

    /// <summary>
    /// The short form description of the plugin.
    /// "A Kuriimu2 plugin."
    /// </summary>
    public string? ShortDescription { get; init; }

    /// <summary>
    /// The website at which either the plugin or the author can be found and contacted.
    /// </summary>
    public string? Website { get; init; }
}