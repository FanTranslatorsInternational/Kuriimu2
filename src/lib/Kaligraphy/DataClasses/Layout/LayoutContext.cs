using Kaligraphy.Contract.DataClasses.Layout;

namespace Kaligraphy.DataClasses.Layout;

public class LayoutContext
{
    public float X { get; set; }
    public float Y { get; set; }

    public float VisibleX { get; set; }

    public IList<TextLayoutLineData> Lines { get; set; } = new List<TextLayoutLineData>();
    public IList<TextLayoutCharacterData> Characters { get; set; } = new List<TextLayoutCharacterData>();
}