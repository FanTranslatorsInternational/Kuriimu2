using Kaligraphy.Contract.DataClasses.Parsing;
using SixLabors.ImageSharp;

namespace Kaligraphy.Contract.DataClasses.Layout;

public class TextLayoutCharacterData
{
    public required CharacterData Character { get; set; }
    public required RectangleF BoundingBox { get; set; }
    public required RectangleF GlyphBoundingBox { get; set; }
}