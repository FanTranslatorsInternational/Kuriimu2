using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Kontract.Attributes;
using Kontract.Interfaces;
using plugin_mt_framework.GFDv1;

namespace plugin_e.x._troopers.Game
{
    [Export(typeof(EXTGameAdapterPS3))]
    [Export(typeof(IGameAdapter))]
    [Export(typeof(IGenerateGamePreviews))]
    [PluginInfo("B344166C-F1BE-49B2-9ADC-38771D0A15DA", "E.X. Troopers (PS3)", "EXTGAPS3", "IcySon55")]
    public sealed class EXTGameAdapterPS3 : IGameAdapter, IGenerateGamePreviews
    {
        private string BasePath => Path.Combine("plugins", ID);

        private readonly Lazy<GFDv1FontAdapter> FontInitializer = new Lazy<GFDv1FontAdapter>(() =>
        {
            var fontPath = Path.Combine("plugins", typeof(EXTGameAdapterPS3).GetCustomAttribute<PluginInfoAttribute>().ID, "jpn", "font00_jpn.gfd");
            var gfd = new GFDv1FontAdapter();
            if (File.Exists(fontPath))
                gfd.Load(fontPath);
            return gfd;
        });
        private GFDv1FontAdapter Font => FontInitializer.Value;

        public string ID => typeof(EXTGameAdapterPS3).GetCustomAttribute<PluginInfoAttribute>().ID;

        public string Name => typeof(EXTGameAdapterPS3).GetCustomAttribute<PluginInfoAttribute>().Name;

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
            var bgPath = Path.Combine(BasePath, "background.png");
            var textBoxPath = Path.Combine(BasePath, "textbox.png");
            var cursorPath = Path.Combine(BasePath, "icon_arrow_talk_ID.png");

            // Setup Bitmaps
            var background = new Bitmap(bgPath);
            background.SetResolution(96, 96);
            var textBox = new Bitmap(textBoxPath);
            var cursor = new Bitmap(cursorPath);

            // Main Kanvas
            var kanvas = new Bitmap(background.Width, background.Height, PixelFormat.Format32bppArgb);

            var lines = 1;
            var fontHeight = Font.Characters.First().GlyphHeight;
            var textBoxOffsetX = 166;
            var textBoxOffsetY = 42;
            var textOffsetX = 66;
            var boxWidth = 400;
            var lineSpacing = 4;

            float x = textBoxOffsetX + textOffsetX, y = 0f;
            float scaleX = 1.0f, scaleY = 1.0f;

            using (var gfx = Graphics.FromImage(kanvas))
            {
                gfx.SmoothingMode = SmoothingMode.HighQuality;
                gfx.InterpolationMode = InterpolationMode.Bicubic;
                gfx.PixelOffsetMode = PixelOffsetMode.HighQuality;

                gfx.DrawImage(background, 0, 0);
                gfx.DrawImage(textBox, textBoxOffsetX, textBoxOffsetY, textBox.Width * 1.1f, textBox.Height * 1.1f);

                // Draw cursor
                int cursorX = 627, cursorY = 161;
                var cursorScale = 0.825f;
                gfx.DrawImage(cursor,
                    new[] {
                        new PointF(cursorX, cursorY),
                        new PointF(cursorX + cursor.Width* cursorScale, cursorY),
                        new PointF(cursorX, cursorY + cursor.Height* cursorScale)
                    },
                    new RectangleF(0, 0, cursor.Width, cursor.Height), GraphicsUnit.Pixel
                );

                // Cleanup text
                var str = Regex.Replace(entry.EditedText, @"<.*?>", "");
                str = str.Replace("\r", "");
                str = str.Replace("\u000A", "\n");
                Font.SetColor(Color.Black);

                // Wrap text
                var results = TextWrapper.WrapText(str, Font, new RectangleF(textBoxOffsetX + textOffsetX, textBoxOffsetY, boxWidth, textBox.Height), scaleX, 0, "\n");
                str = results.Text;
                lines = results.LineCount;

                // Reset
                x = textBoxOffsetX + textOffsetX;
                y = textBoxOffsetY + textBox.Height * 1.1f / 2 - lines * fontHeight / 2 - (lines - 1) * lineSpacing / 2;

                // Draw text
                foreach (var c in str)
                {
                    if (c == '\n' || c == '\u000A')
                    {
                        x = textBoxOffsetX + textOffsetX;
                        y += fontHeight + lineSpacing;
                        continue;
                    }

                    Font.Draw(c, gfx, x, y, scaleX, scaleY);
                    x += Font.GetCharWidthInfo(c).GlyphWidth * scaleX;
                }
            }

            return kanvas;
        }

        private void DrawTransparentImage(Graphics gfx, Bitmap bitmap, float opacity)
        {
            var colorMatrix = new ColorMatrix {Matrix33 = opacity};
            var imgAttribute = new ImageAttributes();
            imgAttribute.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            gfx.DrawImage(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height), 0, 0, bitmap.Width, bitmap.Height, GraphicsUnit.Pixel, imgAttribute);
        }
    }
}
