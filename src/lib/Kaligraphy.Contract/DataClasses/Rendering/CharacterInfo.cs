using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Kaligraphy.Contract.DataClasses.Rendering;

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
}