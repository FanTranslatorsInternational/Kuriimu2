using System.Collections.Generic;
using SixLabors.ImageSharp;

namespace Kuriimu2.ImGui.TextParsing.Models
{
    public record TextLayoutData(IReadOnlyList<TextLayoutLineData> Lines, Rectangle BoundingBox);
}
