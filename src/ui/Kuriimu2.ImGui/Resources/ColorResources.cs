using System.Collections.Generic;
using ImGui.Forms;
using ImGui.Forms.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Kuriimu2.ImGui.Resources
{
    internal static class ColorResources
    {
        private const int ImGuiColMax_ = 55;

        private static readonly Dictionary<Theme, IDictionary<uint, Color>> Store = new()
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

        public static ThemedColor TextSuccessful => new(Store[Theme.Light][ImGuiColMax_ + 1], Store[Theme.Dark][ImGuiColMax_ + 1]);

        public static ThemedColor TextFatal => new(Store[Theme.Light][ImGuiColMax_ + 2], Store[Theme.Dark][ImGuiColMax_ + 2]);

        public static ThemedColor Progress => new(Store[Theme.Light][ImGuiColMax_ + 3], Store[Theme.Dark][ImGuiColMax_ + 3]);

        public static ThemedColor Changed => new(Store[Theme.Light][ImGuiColMax_ + 4], Store[Theme.Dark][ImGuiColMax_ + 4]);

        public static ThemedColor GlyphBackground => new(Color.FromRgba(0xdb, 0xdb, 0xdb, 0xff), Color.FromRgba(0x1d, 0x1d, 0x1d, 0xff));
    }
}
