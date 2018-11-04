using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using Kontract.Attributes;
using Kontract.Interfaces;
using plugin_valkyria_chronicles.SFNT;

namespace plugin_valkyria_chronicles.Game
{
    [Export(typeof(VC3GameAdapter))]
    [Export(typeof(IGameAdapter))]
    [Export(typeof(IGenerateGamePreviews))]
    [PluginInfo("84D2BD62-7AC6-459B-B3BB-3A65855135F6", "Valkyria Chronicles 3", "VC3GA", "IcySon55")]
    public sealed class VC3GameAdapter : IGameAdapter, IGenerateGamePreviews
    {
        private string BasePath => Path.Combine("plugins", ID);

        private static readonly Lazy<SfntFontAdapter> FontInitializer = new Lazy<SfntFontAdapter>(() => new SfntFontAdapter());
        private SfntFontAdapter Font
        {
            get
            {
                if (!FontInitializer.IsValueCreated)
                {
                    var fontPath = Path.Combine(BasePath, "ODIN_FONT_16.BF1");
                    if (File.Exists(fontPath))
                        FontInitializer.Value.Load(fontPath);
                }
                return FontInitializer.Value;
            }
        }

        public string ID => ((PluginInfoAttribute)typeof(VC3GameAdapter).GetCustomAttribute(typeof(PluginInfoAttribute))).ID;

        public string Name => ((PluginInfoAttribute)typeof(VC3GameAdapter).GetCustomAttribute(typeof(PluginInfoAttribute))).Name;

        public string IconPath => Path.Combine("plugins", ID, "icon.png");

        public string Filename { get; set; }

        public IEnumerable<TextEntry> Entries { get; private set; }

        public void LoadEntries(IEnumerable<TextEntry> entries)
        {
            Entries = entries;
        }

        public IEnumerable<TextEntry> SaveEntries()
        {
            return Entries;
        }

        public Bitmap GeneratePreview(TextEntry entry)
        {
            if (!Font.Characters.Any()) return null;

            // Paths
            var bgPath = Path.Combine(BasePath, "BG01.HTX.png");
            if (!File.Exists(bgPath)) return null;

            // Setup
            var bg = new Bitmap(bgPath);
            var img = new Bitmap(bg.Width, bg.Height, PixelFormat.Format32bppArgb);

            // Colors
            var cDefault = Color.FromArgb(90, 47, 22);
            var cNumber = Color.FromArgb(64, 42, 19);

            var textTop = img.Height / 2 + img.Height / 2 / 2;

            var gfx = Graphics.FromImage(img);
            gfx.CompositingMode = CompositingMode.SourceOver;
            gfx.SmoothingMode = SmoothingMode.HighQuality;
            gfx.InterpolationMode = InterpolationMode.Bicubic;
            gfx.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // Begin Drawing
            gfx.DrawImage(bg, 0, 0);
            gfx.FillRectangle(new SolidBrush(Color.White), new Rectangle(16, textTop, 256, 48));

            Font.SetColor(cDefault);
            var lineHeight = Font.Characters.First().GlyphHeight;
            int x = 20, y = textTop + 4;
            foreach (var c in entry.EditedText)
            {
                // Handle line break
                if (c == '\n')
                {
                    x = 20;
                    y += lineHeight;
                    continue;
                }

                if (c >= '0' && c <= '9')
                    Font.SetColor(cNumber);
                else
                    Font.SetColor(cDefault);

                Font.Draw(c, gfx, x, y, 1.0f, 1.0f);
                x += Font.GetCharWidthInfo(c).GlyphWidth;
            }

            return img;
        }
    }
}
