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
        private const string PluginDirectory = "plugins";
        private string BasePath => Path.Combine("plugins", ID);

        public string ID => typeof(VC3GameAdapter).GetCustomAttribute<PluginInfoAttribute>().ID;

        public string Name => typeof(VC3GameAdapter).GetCustomAttribute<PluginInfoAttribute>().Name;

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

        #region Resources

        private readonly Lazy<SfntFontAdapter> OdinFontInitializer = new Lazy<SfntFontAdapter>(() =>
        {
            var fontPath = Path.Combine("plugins", typeof(VC3GameAdapter).GetCustomAttribute<PluginInfoAttribute>().ID, "ODIN_FONT_16.BF1");
            var sfnt = new SfntFontAdapter();
            if (File.Exists(fontPath))
                sfnt.Load(fontPath);
            return sfnt;
        });
        private SfntFontAdapter OdinFont => OdinFontInitializer.Value;

        private readonly Lazy<Bitmap> BackgroundInit = new Lazy<Bitmap>(() =>
        {
            var backgroundPath = Path.Combine(PluginDirectory, typeof(VC3GameAdapter).GetCustomAttribute<PluginInfoAttribute>().ID, "BG01.HTX.png");
            var bg = new Bitmap(backgroundPath);
            bg.SetResolution(96, 96);
            return bg;
        });
        private Bitmap background => BackgroundInit.Value;

        #endregion

        public Bitmap GeneratePreview(TextEntry entry)
        {
            if (!OdinFont.Characters.Any()) return null;

            // Setup SQLite
            var mtpSql = $"Data Source={Path.Combine(BasePath, "mtp.sqlite")};Version=3;";
            var mxeSql = $"Data Source={Path.Combine(BasePath, "mxec.sqlite")};Version=3;";

            if (Filename.Contains("MTP"))
                SQLiteDB.ConnectionString = mtpSql;
            else if (Filename.Contains("MXE"))
                SQLiteDB.ConnectionString = mxeSql;

            // Main Kanvas
            var kanvas = new Bitmap(background.Width, background.Height, PixelFormat.Format32bppArgb);

            // Colors
            var cDefault = Color.FromArgb(90, 47, 22);
            var cNumber = Color.FromArgb(64, 42, 19);

            var gfx = Graphics.FromImage(kanvas);
            gfx.SmoothingMode = SmoothingMode.None;
            gfx.InterpolationMode = InterpolationMode.NearestNeighbor;
            gfx.PixelOffsetMode = PixelOffsetMode.Half;

            // Load Files
            var balloonLokiHard = new Dicer(Path.Combine(BasePath, "BALLOON_LOKI_HARD.HTX.png"));

            // Scene Variables
            var scene = 1; // Make an enum
            int x = 0, y = 0, xR = 0;
            float scaleX = 0.75f, scaleY = 0.75f;
            var lineHeight = OdinFont.Characters.First().GlyphHeight + 3;

            var template = new Bitmap(Path.Combine(BasePath, "ULJM05781_00044.png"));

            // Render Scene
            switch (scene)
            {
                case 1:
                    OdinFont.SetColor(cDefault);

                    gfx.DrawImage(background, 0, 0);

                    var lines = entry.EditedText.Split('\n').Length;

                    var balloonX = 86;
                    var balloonY = 127;
                    DrawBalloon(gfx, balloonLokiHard, new Point(balloonX, balloonY), lines);

                    var textX = balloonX + 5;
                    var textY = balloonY;

                    switch (lines)
                    {
                        case 1:
                            textY += OdinFont.Characters.First().GlyphHeight / 2 + 1;
                            break;
                        case 3:
                            textY += OdinFont.Characters.First().GlyphHeight / 2 - 2;
                            break;
                        case 2:
                        case 4:

                            break;
                    }

                    // Set text box
                    x = xR = textX;
                    y = textY;
                    scaleX = 0.93f;
                    scaleY = 0.875f;

                    //DrawTransparentImage(gfx, template, 0.5f);

                    break;
            }

            gfx.SmoothingMode = SmoothingMode.None;
            gfx.InterpolationMode = InterpolationMode.Low;
            //gfx.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // Render Text
            if (entry != null)
                foreach (var c in entry.EditedText.Replace("\r", string.Empty))
                {
                    // Handle line break
                    if (c == '\n')
                    {
                        x = xR;
                        y += lineHeight;
                        continue;
                    }

                    //// Colored Numbers
                    //if (c >= '0' && c <= '9')
                    //    OdinFont.SetColor(cNumber);
                    //else
                    //    OdinFont.SetColor(cDefault);

                    OdinFont.Draw(c, gfx, x, y, scaleX, scaleY);
                    x += (int)(OdinFont.GetCharWidthInfo(c).GlyphWidth * scaleX);
                }

            return kanvas;
        }

        private void DrawBalloon(Graphics gfx, Dicer dicer, Point location, int lines)
        {
            var grid = dicer.Slice.UserInt1;
            var corner = lines == 1 ? "SmallCorner" : "BigCorner";
            var height = lines == 1 ? 2 : 4;

            dicer.DrawSlice(corner, gfx, location.X - 4 * grid, location.Y - 2 * grid);
            dicer.DrawSlice(corner, gfx, location.X - 4 * grid + grid * 17, location.Y - 2 * grid, true, true);

            dicer.DrawSlice("Edge", gfx, location.X, location.Y - grid, false, true, grid * 13);
            dicer.DrawSlice("Edge", gfx, location.X, location.Y + grid * height, false, false, grid * 13);
            
            dicer.DrawSlice("Fill", gfx, location.X, location.Y, false, false, grid * 13, grid * height);
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
