using Kaligraphy.Enums.Layout;

namespace Kaligraphy.DataClasses.Layout;

public class LayoutOptions
{
    public HorizontalTextAlignment HorizontalAlignment { get; init; } = HorizontalTextAlignment.Left;
    public VerticalTextAlignment VerticalAlignment { get; init; } = VerticalTextAlignment.Top;
    public int LineHeight { get; init; }
    public int LineWidth { get; init; }
    public float TextScale { get; init; } = 1f;
    public float TextSpacing { get; init; } = 1;
}