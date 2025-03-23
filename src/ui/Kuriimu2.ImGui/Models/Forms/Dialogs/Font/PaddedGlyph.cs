using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Kuriimu2.ImGui.Models.Forms.Dialogs.Font
{
    internal class PaddedGlyph
    {
        public Image<Rgba32> Glyph { get; set; }
        public int Baseline { get; set; }
        public int PaddingLeft { get; set; }
        public int PaddingRight { get; set; }
    }
}
