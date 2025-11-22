using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace Konnect.Contract.DataClasses.Plugin.File.Font;

/// <summary>
/// A class representing one character of a font.
/// </summary>
public class CharacterInfo
{
    /// <summary>
    /// The code point of this character.
    /// </summary>
    public required char CodePoint { get; init; }

    /// <summary>
    /// The bounding box the glyph.
    /// </summary>
    public required Size BoundingBox { get; set; }

    /// <summary>
    /// The position relative to the bounding box to draw the glyph at.
    /// </summary>
    public required Point GlyphPosition { get; set; }

    /// <summary>
    /// The glyph of the character, if any.
    /// </summary>
    public Image<Rgba32>? Glyph { get; set; }

    /// <summary>
    /// Determines if the content of this character was changed.
    /// </summary>
    /// <remarks>Should only be set by this class or the responsible plugin.</remarks>
    public bool ContentChanged { get; set; }
}