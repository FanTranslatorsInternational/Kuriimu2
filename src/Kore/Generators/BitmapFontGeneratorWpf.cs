#if !NET_CORE_21
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Kontract.Interfaces.Font;

namespace Kore.Generators
{
    // TODO: Is this needed in Kore still?
    public class BitmapFontGeneratorWpf
    {
        // Input
        public int GlyphHeight { get; set; } = 50;
        public int GlyphPadding { get; set; } = 0;
        public int Baseline { get; set; } = 30;

        public int MaxCanvasWidth { get; set; } = 1024;
        public int MaxCanvasHeight { get; set; } = 512;

        public Typeface Typeface { get; set; } = null;
        public double FontSize { get; set; } = 32;

        // Output
        public IFontAdapter Adapter { get; set; } = null;
        public IEnumerable<DrawingImage> Textures { get; private set; } = null;
        public IEnumerable<FontCharacter> Characters { get; private set; } = null;

        public BitmapFontGeneratorWpf()
        {

        }

        public BitmapFontGeneratorWpf(int maxCanvasWidth, int maxCanvasHeight)
        {
            MaxCanvasWidth = maxCanvasWidth;
            MaxCanvasHeight = maxCanvasHeight;
        }

        public void Generate(List<ushort> characters)
        {
            if (Typeface == null)
                throw new InvalidOperationException("No typeface was specified.");

            if (!Typeface.TryGetGlyphTypeface(out var glyphTypeface))
                throw new InvalidOperationException("No glyph typeface was found.");

            if (characters == null || characters.Count == 0)
                throw new InvalidOperationException("No characters were specified.");

            // Clear out the existing characters
            Adapter.Characters = new List<FontCharacter>();

            // Setup
            var glyphs = new List<ushort>();
            var textures = new List<DrawingImage>();
            var dg = new DrawingGroup();
            var dc = dg.Open();

            var glyphOffsets = new List<Point>();
            var advanceWidths = new List<double>();

            var current = new Point(0, 0);

            foreach (var character in characters)
            {
                ushort glyphIndex;

                try
                {
                    glyphIndex = glyphTypeface.CharacterToGlyphMap[character];
                }
                catch
                {
                    continue;
                }

                var rect = glyphTypeface.GetGlyphOutline(glyphIndex, FontSize, FontSize).Bounds;

                if (double.IsInfinity(rect.Width))
                    rect = new Rect(new Size(FontSize / 3.33, GlyphHeight));

                rect.Width += GlyphPadding * 2;

                // Next Line Logic
                if (current.X + rect.Width > MaxCanvasWidth)
                {
                    current.X = 0;
                    current.Y += GlyphHeight;

                    if (current.Y + GlyphHeight + GlyphPadding > MaxCanvasHeight)
                        current.Y = 0;
                }

                // Add Glyph
                glyphs.Add(glyphIndex);
                glyphOffsets.Add(new Point(
                    current.X - glyphTypeface.LeftSideBearings[glyphIndex] * FontSize,
                    -current.Y - glyphTypeface.Height * FontSize
                ));
                advanceWidths.Add(0);

                rect.X = current.X - GlyphPadding;
                rect.Y = current.Y + rect.Top + (glyphTypeface.Height * FontSize); // - glyphTypeface.TopSideBearings[glyphIndex] * fontSize);

                // Add Character
                if (Adapter is IAddCharacters add)
                {
                    var fc = add.NewCharacter();
                    fc.Character = character;
                    fc.TextureID = 0;
                    fc.GlyphX = (int)current.X; // This doesn't appear to work as expected
                    fc.GlyphY = (int)current.Y; // This doesn't appear to work as expected
                    fc.GlyphWidth = (int)rect.Width;
                    fc.GlyphHeight = (int)rect.Height;
                    add.AddCharacter(fc);
                }

                // Move cursor
                current.X += rect.Width;

                dc.DrawRectangle(Brushes.Transparent, new Pen(Brushes.Red, 1), rect);
            }

            var glyphRun = new GlyphRun(glyphTypeface, 0, false, FontSize, glyphs, new Point(0, 0), advanceWidths, glyphOffsets, null, null, null, null, null);

            dc.DrawGlyphRun(Brushes.White, glyphRun);
            dc.Close();

            textures.Add(new DrawingImage(dg));

            // Set output variables
            // TODO: This should set both Characters and Textures on the Adapter
            if (Adapter != null)
                Characters = Adapter.Characters;
            Textures = textures;
        }

    }
}
#endif
