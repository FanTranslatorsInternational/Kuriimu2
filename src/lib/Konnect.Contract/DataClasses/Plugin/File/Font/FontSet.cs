namespace Konnect.Contract.DataClasses.Plugin.File.Font;

/// <summary>
/// Represents a full set of characters in a font.
/// </summary>
public class FontSet
{
    /// <summary>
    /// The list of characters provided by the state.
    /// </summary>
    public required IReadOnlyList<CharacterInfo> Characters { get; init; }
}