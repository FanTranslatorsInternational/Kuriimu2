using System.Collections.Generic;
using ImGui.Forms;
using ImGui.Forms.Models;
using ImGuiNET;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Kuriimu2.ImGui.Resources
{
    static class ColorResources
    {
        private const int ImGuiColMax_ = 55;

        private static readonly IDictionary<Theme, IDictionary<uint, Color>> Store = new Dictionary<Theme, IDictionary<uint, Color>>
        {
            [Theme.Dark] = new Dictionary<uint, Color>
            {
                [ImGuiColMax_ + 1] = new Rgba32(0x49, 0xe7, 0x9a),
                [ImGuiColMax_ + 2] = new Rgba32(0xcf, 0x66, 0x79),
                [ImGuiColMax_ + 3] = Color.ForestGreen,
                [ImGuiColMax_ + 4] = new Rgba32(0xFF, 0xA5, 0x00)
            },
            [Theme.Light] = new Dictionary<uint, Color>
            {
                [ImGuiColMax_ + 1] = Color.ForestGreen,
                [ImGuiColMax_ + 2] = Color.DarkRed,
                [ImGuiColMax_ + 3] = Color.ForestGreen,
                [ImGuiColMax_ + 4] = new Rgba32(0xFF, 0xA5, 0x00)
            }
        };

        public static Color TextSuccessful => Store[Style.Theme][ImGuiColMax_ + 1];

        public static Color TextFatal => Store[Style.Theme][ImGuiColMax_ + 2];

        public static Color Progress => Store[Style.Theme][ImGuiColMax_ + 3];

        public static Color Changed => Store[Style.Theme][ImGuiColMax_ + 4];

        public static ThemedColor GlyphBackground => new(Color.FromRgba(0xdb, 0xdb, 0xdb, 0xff), Color.FromRgba(0x1d, 0x1d, 0x1d, 0xff));
    }
}
