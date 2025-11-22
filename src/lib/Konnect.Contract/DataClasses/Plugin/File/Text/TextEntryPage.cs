namespace Konnect.Contract.DataClasses.Plugin.File.Text;

/// <summary>
/// Presents a collection of <see cref="TextEntry"/> that represent a single page.
/// </summary>
public class TextEntryPage
{
    /// <summary>
    /// The name of the page.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// The <see cref="TextEntry"/>s that represent a single page.
    /// </summary>
    public IList<TextEntry> Entries { get; init; }
}