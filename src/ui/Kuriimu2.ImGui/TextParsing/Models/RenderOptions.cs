using SixLabors.ImageSharp;

namespace Kuriimu2.ImGui.TextParsing.Models
{
    public class RenderOptions
    {
        public bool DrawBoundingBoxes { get; set; }

        public int VisibleLines { get; set; }
        public int OutlineRadius { get; set; }

        public Color TextColor { get; set; } = Color.Black;
        public Color TextOutlineColor { get; set; } = Color.Transparent;
    }
}
