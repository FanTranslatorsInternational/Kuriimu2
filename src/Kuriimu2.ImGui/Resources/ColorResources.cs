using System;
using System.Collections.Generic;
using System.Drawing;
using ImGui.Forms.Extensions;
using ImGui.Forms.Models;
using ImGuiNET;

namespace Kuriimu2.ImGui.Resources
{
    static class ColorResources
    {
        private const int ImGuiColMax_ = 55;

        private static readonly IDictionary<Theme, IDictionary<uint, Color>> Store = new Dictionary<Theme, IDictionary<uint, Color>>
        {
            [Theme.Dark] = new Dictionary<uint, Color>
            {
                [ImGuiColMax_ + 1] = Color.FromArgb(0x49, 0xe7, 0x9a),
                [ImGuiColMax_ + 2] = Color.FromArgb(0xcf, 0x66, 0x79),
                [ImGuiColMax_ + 3] = Color.ForestGreen,
                [ImGuiColMax_ + 4] = Color.FromArgb(0xFF, 0xA5, 0x00)
            },
            [Theme.Light] = new Dictionary<uint, Color>
            {
                [ImGuiColMax_ + 1] = Color.ForestGreen,
                [ImGuiColMax_ + 2] = Color.DarkRed,
                [ImGuiColMax_ + 3] = Color.ForestGreen,
                [ImGuiColMax_ + 4] = Color.FromArgb(0xFF, 0xA5, 0x00)
            }
        };

        public static Color TextDefault => ImGuiNET.ImGui.GetColorU32(ImGuiCol.Text).ToColor();

        public static Color TextSuccessful => Store[GetTheme()][ImGuiColMax_ + 1];

        public static Color TextFatal => Store[GetTheme()][ImGuiColMax_ + 2];

        public static Color Progress => Store[GetTheme()][ImGuiColMax_ + 3];

        public static Color ArchiveChanged => Store[GetTheme()][ImGuiColMax_ + 4];

        private static Theme GetTheme() => Enum.Parse<Theme>(Settings.Default.Theme);
    }
}
