using System;
using System.Collections.Generic;
using System.Drawing;
using ImGui.Forms.Models;

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
                [ImGuiColMax_ + 3] = Color.ForestGreen
            },
            [Theme.Light] = new Dictionary<uint, Color>
            {
                [ImGuiColMax_ + 1] = Color.ForestGreen,
                [ImGuiColMax_ + 2] = Color.DarkRed,
                [ImGuiColMax_ + 3] = Color.ForestGreen
            }
        };

        public static Color TextSuccessful => Store[GetTheme()][ImGuiColMax_ + 1];

        public static Color TextFatal => Store[GetTheme()][ImGuiColMax_ + 2];

        public static Color Progress => Store[GetTheme()][ImGuiColMax_ + 3];

        private static Theme GetTheme() => Enum.Parse<Theme>(Settings.Default.Theme);
    }
}
