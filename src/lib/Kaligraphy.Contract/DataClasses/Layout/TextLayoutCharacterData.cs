using Kaligraphy.Contract.DataClasses.Parsing;
using SixLabors.ImageSharp;

namespace Kaligraphy.Contract.DataClasses.Layout;

public class TextLayoutCharacterData
{
    public CharacterData Character { get; set; }
    public RectangleF BoundingBox { get; set; }
    public RectangleF GlyphBoundingBox { get; set; }
}