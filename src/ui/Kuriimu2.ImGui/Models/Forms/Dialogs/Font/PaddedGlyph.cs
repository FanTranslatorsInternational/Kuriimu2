using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Kuriimu2.ImGui.Models.Forms.Dialogs.Font
{
    internal class PaddedGlyph
    {
        public Image<Rgba32> Glyph { get; set; }
        public Size BoundingBox { get; set; }
        public Point GlyphPosition { get; set; }
        public int Baseline { get; set; }
    }
}
