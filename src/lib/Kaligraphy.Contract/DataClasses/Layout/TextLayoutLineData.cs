using SixLabors.ImageSharp;

namespace Kaligraphy.Contract.DataClasses.Layout;

public class TextLayoutLineData
{
    public required IList<TextLayoutCharacterData> Characters { get; set; }
    public required RectangleF BoundingBox { get; set; }
}