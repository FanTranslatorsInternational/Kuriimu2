using SixLabors.ImageSharp;

namespace Kaligraphy.DataClasses.Rendering;

public class RenderOptions
{
    public bool DrawBoundingBoxes { get; set; }

    public int VisibleLines { get; set; }
    public int OutlineRadius { get; set; }

    public Color TextColor { get; set; } = Color.White;
    public Color TextOutlineColor { get; set; } = Color.Transparent;
}