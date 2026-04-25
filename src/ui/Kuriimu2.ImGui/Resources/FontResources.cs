using System;
using ImGui.Forms.Factories;
using ImGui.Forms.Resources;

namespace Kuriimu2.ImGui.Resources
{
    static class FontResources
    {
        public static void RegisterFonts()
        {
            FontFactory.RegisterFromResource("Roboto", "roboto.ttf");
            FontFactory.RegisterFromResource("NotoJp", "notojp.ttf");
            FontFactory.RegisterFromResource("NotoKr", "notokr.ttf");
            FontFactory.RegisterFromResource("NotoZhTc", "notozhtc.ttf");
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
