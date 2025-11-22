using SixLabors.ImageSharp;

namespace Kaligraphy.Contract.DataClasses;

public class GlyphDescriptionData
{
    /// <summary>
    /// The position into the glyph.
    /// </summary>
    public required Point Position { get; init; }

    /// <summary>
    /// The size of the glyph.
    /// </summary>
    public required Size Size { get; init; }
}