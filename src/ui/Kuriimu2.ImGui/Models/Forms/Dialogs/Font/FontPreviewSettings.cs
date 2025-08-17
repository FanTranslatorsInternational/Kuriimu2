using Kaligraphy.Enums.Layout;

namespace Kuriimu2.ImGui.Models.Forms.Dialogs.Font
{
    class FontPreviewSettings
    {
        public bool ShowDebugBoxes { get; set; }
        public int Spacing { get; set; } = 1;
        public int LineHeight { get; set; }
        public HorizontalTextAlignment HorizontalAlignment { get; set; } = HorizontalTextAlignment.Left;
    }
}
