using System;
using ImGui.Forms.Factories;
using ImGui.Forms.Models;
using ImGui.Forms.Resources;

namespace Kuriimu2.ImGui.Resources
{
    static class FontResources
    {
        public static void RegisterFonts()
        {
            FontFactory.RegisterFromResource("Roboto", "roboto.ttf", FontGlyphRange.Latin | FontGlyphRange.Cyrillic | FontGlyphRange.Greek);
            FontFactory.RegisterFromResource("NotoJp", "notojp.ttf", FontGlyphRange.ChineseJapanese);
            FontFactory.RegisterFromResource("NotoKr", "notokr.ttf", FontGlyphRange.Korean);
            FontFactory.RegisterFromResource("NotoZhTc", "notozhtc.ttf", FontGlyphRange.ChineseJapanese);
        }

        public static FontResource GetFont(FontType type, int size)
        {
            switch (type)
            {
                case FontType.Application:
                    return FontFactory.Get("Roboto", size, FontFactory.Get("NotoJp", size, FontFactory.Get("NotoKr", size, FontFactory.Get("NotoZhTc", size))));

                case FontType.Hexadecimal:
                    return FontFactory.GetDefault(size);

                default:
                    throw new InvalidOperationException($"Invalid font type {type}.");
            }
        }
    }

    enum FontType
    {
        Application,
        Hexadecimal
    }
}
