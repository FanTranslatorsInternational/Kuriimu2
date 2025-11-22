using System.Text;

namespace Konnect.Contract.DataClasses.Plugin.File.Text;

/// <summary>
/// The base class for pages.
/// </summary>
public class TextEntry
{
    /// <summary>
    /// The name for this entry.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The encoding for the text data.
    /// </summary>
    public required Encoding Encoding { get; init; }

    /// <summary>
    /// The text data in bytes for this entry.
    /// </summary>
    public required byte[] TextData { get; set; }

    /// <summary>
    /// Determines if the content of this entry was modified.
    /// </summary>
    public bool ContentChanged { get; set; }
}