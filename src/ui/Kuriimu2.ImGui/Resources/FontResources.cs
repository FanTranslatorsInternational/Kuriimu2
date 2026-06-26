using System;
using System.Reflection;
using ImGui.Forms.Factories;
using ImGui.Forms.Resources;

namespace Kuriimu2.ImGui.Resources
{
    internal static class FontResources
    {
        public static void RegisterFonts()
        {
            Assembly assembly = typeof(FontResources).Assembly;

            FontFactory.RegisterFromResource("Roboto", assembly, "roboto.ttf");
            FontFactory.RegisterFromResource("NotoJp", assembly, "notojp.ttf");
            FontFactory.RegisterFromResource("NotoKr", assembly, "notokr.ttf");
            FontFactory.RegisterFromResource("NotoZhTc", assembly, "notozhtc.ttf");
            FontFactory.RegisterFromResource("NotoArab", assembly, "notoar.ttf");
        }

        public static FontResource GetFont(FontType type, int size)
        {
            return type switch
            {
                FontType.Application => FontFactory.Get("Roboto", size, FontFactory.Get("NotoJp", size, FontFactory.Get("NotoKr", size, FontFactory.Get("NotoZhTc", size, FontFactory.Get("NotoArab", size))))),
                FontType.Hexadecimal => FontFactory.GetDefault(size),
                _ => throw new InvalidOperationException($"Invalid font type {type}.")
            };
        }
    }

    internal enum FontType
    {
        Application,
        Hexadecimal
    }
}
