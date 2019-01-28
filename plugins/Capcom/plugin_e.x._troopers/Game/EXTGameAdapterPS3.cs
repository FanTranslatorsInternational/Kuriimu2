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
using Kontract.Interfaces.Game;
using Kontract.Interfaces.Text;
using plugin_mt_framework.GFDv1;

namespace plugin_e.x._troopers.Game
{
    [Export(typeof(EXTGameAdapterPS3))]
    [Export(typeof(IGameAdapter))]
    [Export(typeof(IGenerateGamePreviews))]
    [PluginInfo("B344166C-F1BE-49B2-9ADC-38771D0A15DA", "E.X. Troopers (PS3)", "EXTGAPS3", "IcySon55")]
    public sealed class EXTGameAdapterPS3 : IGameAdapter, IGenerateGamePreviews
    {
        private const string PluginDirectory = "plugins";

        public string ID { get; } = typeof(EXTGameAdapterPS3).GetCustomAttribute<PluginInfoAttribute>().ID;

        public static string _ID { get; } = typeof(EXTGameAdapterPS3).GetCustomAttribute<PluginInfoAttribute>().ID;

        public string Name { get; } = typeof(EXTGameAdapterPS3).GetCustomAttribute<PluginInfoAttribute>().Name;

        public string IconPath { get; } = Path.Combine(PluginDirectory, _ID, "icon.png");

        public string Filename { get; set; }

        #region Text IO

        public IEnumerable<TextEntry> Entries { get; private set; }

        public void LoadEntries(IEnumerable<TextEntry> entries)
        {
            Entries = entries;
        }

        public IEnumerable<TextEntry> SaveEntries()
        {
            return Entries;
        }

        #endregion

        #region Resources

        private static readonly Lazy<GFDv1FontAdapter> FontInitializer = new Lazy<GFDv1FontAdapter>(() =>
        {
            var fontPath = Path.Combine(PluginDirectory, _ID, "jpn", "font00_jpn.gfd");
            var gfd = new GFDv1FontAdapter();
            if (File.Exists(fontPath))
                gfd.Load(fontPath);
            return gfd;
        });
        private static GFDv1FontAdapter Font => FontInitializer.Value;

        private static readonly Lazy<GFDv1FontAdapter> PadFontInitializer = new Lazy<GFDv1FontAdapter>(() =>
        {
            var fontPath = Path.Combine(PluginDirectory, _ID, "jpn", "pad.gfd");
            var gfd = new GFDv1FontAdapter();
            if (File.Exists(fontPath))
                gfd.Load(fontPath);
            return gfd;
        });
        private static GFDv1FontAdapter PadFont => PadFontInitializer.Value;

        private static readonly Lazy<Bitmap> BackgroundInit = new Lazy<Bitmap>(() =>
        {
            var backgroundPath = Path.Combine(PluginDirectory, _ID, "background.png");
            var bg = new Bitmap(backgroundPath);
            bg.SetResolution(96, 96);
            return bg;
        });
        private static Bitmap background => BackgroundInit.Value;

        private static readonly Lazy<Bitmap> TutorialInit = new Lazy<Bitmap>(() =>
        {
            var backgroundPath = Path.Combine(PluginDirectory, _ID, "tutorial.png");
            var bg = new Bitmap(backgroundPath);
            bg.SetResolution(96, 96);
            return bg;
        });
        private static Bitmap tutorial => TutorialInit.Value;

        private static readonly Lazy<Bitmap> WizpediaInit = new Lazy<Bitmap>(() =>
        {
            var backgroundPath = Path.Combine(PluginDirectory, _ID, "wizpedia.png");
            var bg = new Bitmap(backgroundPath);
            bg.SetResolution(96, 96);
            return bg;
        });
        private static Bitmap wizpedia => WizpediaInit.Value;

        private static readonly Lazy<Bitmap> TextBoxInit = new Lazy<Bitmap>(() =>
        {
            var textBoxPath = Path.Combine(PluginDirectory, _ID, "textbox.png");
            var bg = new Bitmap(textBoxPath);
            bg.SetResolution(96, 96);
            return bg;
        });
        private static Bitmap textBox => TextBoxInit.Value;

        private static readonly Lazy<Bitmap> CursorInit = new Lazy<Bitmap>(() =>
        {
            var cursorPath = Path.Combine(PluginDirectory, _ID, "cursor.png");
            var bg = new Bitmap(cursorPath);
            bg.SetResolution(96, 96);
            return bg;
        });
        private static Bitmap cursor => CursorInit.Value;

        #endregion

        private enum Scene
        {
            General,
            Tutorial,
            Wizpedia
        }

        public Bitmap GeneratePreview(TextEntry entry)
        {
            if (!Font.Characters.Any()) return null;

            // Globals
            var scene = Scene.General;
            if (Filename.Contains("tutorial_jpn")) scene = Scene.Tutorial;
            if (Filename.Contains("wizpedia_jpn")) scene = Scene.Wizpedia;

            var pad = new Dictionary<string, char>
            {
                ["PAD_A"] = 'a', // Circle
                ["PAD_B"] = 'b', // Cross
                ["PAD_X"] = 'c', // Triangle
                ["PAD_Y"] = 'd', // Square
                ["PAD_L"] = 'e', // L1
                ["PAD_R"] = 'f', // R1
                //["PAD_G"] = 'g', // D-Pad
                //["PAD_H"] = 'h', // D-Pad Up
                ["PAD_ADOWN"] = 'i', // D-Pad Down
                //["PAD_J"] = 'j', // D-Pad Left
                //["PAD_K"] = 'k', // D-Pad Right
                ["PAD_CLR"] = 'l', // D-Pad Left & Right
                //["PAD_CUD"] = 'm', // D-Pad Up & Down
                ["PAD_ANALOG"] = 'n', // Left Analog Stick
                ["PAD_AUP"] = 'o', // Right Analog Stick
                ["PAD_ALR"] = 't', // Left/Right Analog Stick
                //["PAD_U"] = 'u' // Hand Cursor
                //["PAD_V"] = 'v' // Plus Sign
                //["PAD_W"] = 'w' // Start
                //["PAD_X"] = 'x' // Select
                ["PAD_L2"] = 'y', // L2
                ["PAD_R2"] = 'z', // R2
            };

            var genSize = 46f;
            var tutSize = 36f;
            var wizSize = 32.25f;

            // Main Kanvas
            var kanvas = new Bitmap(background.Width, background.Height, PixelFormat.Format32bppArgb);
            kanvas.SetResolution(96, 96);

            var lines = 1;
            var fontHeight = Font.Characters.First().GlyphHeight;
            var textBoxOffsetX = 166f;
            var textBoxOffsetY = 42;
            var textOffsetX = 66;
            var boxWidth = 400;
            var lineSpacing = 4f;
            var size = genSize;

            var magic = 0.02173913043478260869565217391304f; // 0.04166666666666666666666666666667

            float x = textBoxOffsetX + textOffsetX, y = 0;
            float scaleX = 1.0f, scaleY = 1.0f;

            using (var gfx = Graphics.FromImage(kanvas))
            {
                gfx.SmoothingMode = SmoothingMode.HighQuality;
                gfx.InterpolationMode = InterpolationMode.Bicubic;
                gfx.PixelOffsetMode = PixelOffsetMode.HighQuality;

                // Cleanup text
                var str = entry.EditedText.Replace("\r", "");
                Font.SetColor(Color.Black);

                switch (scene)
                {
                    case Scene.General:
                        gfx.DrawImage(background, 0, 0);
                        gfx.DrawImage(textBox, textBoxOffsetX, textBoxOffsetY, textBox.Width * 1.1f, textBox.Height * 1.1f);

                        // Cursor
                        int cursorX = 627, cursorY = 161;
                        var cursorScale = 0.825f;
                        gfx.DrawImage(cursor,
                            new[]
                            {
                                new PointF(cursorX, cursorY),
                                new PointF(cursorX + cursor.Width * cursorScale, cursorY),
                                new PointF(cursorX, cursorY + cursor.Height * cursorScale)
                            },
                            new RectangleF(0, 0, cursor.Width, cursor.Height), GraphicsUnit.Pixel
                        );

                        // Wrap text
                        var results = TextWrapper.WrapText(str, Font, PadFont, new RectangleF(textBoxOffsetX + textOffsetX, textBoxOffsetY, boxWidth, textBox.Height), scaleX, 0, "\n");
                        str = results.Text;
                        lines = results.LineCount;
                        size = genSize;

                        // Set
                        x = textBoxOffsetX + textOffsetX;
                        y = textBoxOffsetY + textBox.Height * 1.1f / 2 - lines * fontHeight / 2 - (lines - 1) * lineSpacing / 2;

                        break;
                    case Scene.Tutorial:
                        gfx.DrawImage(tutorial, 0, 0);
                        
                        // Configure
                        textBoxOffsetX = 368.5f;
                        textOffsetX = 0;
                        textBoxOffsetY = 465;
                        lineSpacing = 2;
                        size = tutSize;

                        // Set
                        x = textBoxOffsetX + textOffsetX;
                        y = textBoxOffsetY;
                        break;
                    case Scene.Wizpedia:
                        gfx.DrawImage(wizpedia, 0, 0);

                        // Template
                        //DrawTransparentImage(gfx, new Bitmap(Path.Combine(PluginDirectory, _ID, "wizpedia_template.png")), 0.25f);

                        // Configure
                        textBoxOffsetX = 695f;
                        textOffsetX = 0;
                        textBoxOffsetY = 302;
                        lineSpacing = -1.25f;
                        size = wizSize;

                        // Set
                        x = textBoxOffsetX + textOffsetX;
                        y = textBoxOffsetY;
                        break;
                }

                // Draw text
                for (var i = 0; i < str.Length; i++)
                {
                    var c = str[i];

                    if (c == '<')
                    {
                        var tag = Regex.Match(str.Substring(i), @"</?(\w+) ?(.*?)>").Value;
                        var code = Regex.Match(tag, @"(?<=</?)\w+").Value;
                        var isCloser = Regex.Match(tag, @"</\w+").Value.StartsWith("</");

                        switch (code)
                        {
                            case "COL":
                                Font.SetColor(isCloser ? Color.Black : ColorTranslator.FromHtml("#" + Regex.Match(tag, @"[0-9A-F]{6}").Value));
                                break;
                            case "SIZE":
                                if (isCloser)
                                {
                                    if (scene == Scene.General)
                                        size = genSize;
                                    else if (scene == Scene.Tutorial)
                                        size = tutSize;
                                    else if (scene == Scene.Wizpedia)
                                        size = wizSize;
                                }
                                else
                                    size = Convert.ToInt32(Regex.Match(tag, @"(?<= )\d+").Value);
                                break;
                            case "ICON":
                                var icon = Regex.Match(tag, @"(?<= )\w+").Value;
                                var p = pad[icon];
                                var scale = size * magic; // Magic scaling value

                                PadFont.Draw(p, gfx, x, y, scale, scale);
                                x += PadFont.GetCharWidthInfo(p).GlyphWidth * scale;
                                break;
                            case "WIZP":
                                Font.SetColor(isCloser ? Color.Black : Color.Red);
                                break;
                        }

                        i += tag.Length - 1;
                    }
                    else
                    {
                        if (c == '\n')
                        {
                            x = textBoxOffsetX + textOffsetX;
                            y += fontHeight + lineSpacing;
                            continue;
                        }

                        Font.Draw(c, gfx, x, y, size * magic, size * magic);
                        x += Font.GetCharWidthInfo(c).GlyphWidth * (size * magic);
                    }
                }
            }

            return kanvas;
        }

        private void DrawTransparentImage(Graphics gfx, Bitmap bitmap, float opacity)
        {
            var colorMatrix = new ColorMatrix { Matrix33 = opacity };
            var imgAttribute = new ImageAttributes();
            imgAttribute.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            gfx.DrawImage(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height), 0, 0, bitmap.Width, bitmap.Height, GraphicsUnit.Pixel, imgAttribute);
        }
    }
}
