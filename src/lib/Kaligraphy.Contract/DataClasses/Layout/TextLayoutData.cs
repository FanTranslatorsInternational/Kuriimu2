using SixLabors.ImageSharp;

namespace Kaligraphy.Contract.DataClasses.Layout;

public record TextLayoutData(IReadOnlyList<TextLayoutLineData> Lines, RectangleF BoundingBox);