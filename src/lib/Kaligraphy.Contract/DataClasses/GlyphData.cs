using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace Kaligraphy.Contract.DataClasses;

public class GlyphData
{
    /// <summary>
    /// The character this glyph represents.
    /// </summary>
    public required char Character { get; init; }

    /// <summary>
    /// The glyph.
    /// </summary>
    public required Image<Rgba32> Glyph { get; init; }

    /// <summary>
    /// Gets a description of the glyph, including position and size of the glyph to be rendered.
    /// </summary>
    public required GlyphDescriptionData Description { get; init; }
}