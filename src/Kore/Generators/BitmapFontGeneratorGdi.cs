using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Kontract.Interfaces;

namespace Kore.Generators
{
    public class BitmapFontGeneratorGdi
    {
        // Input
        public IFontAdapter Adapter { get; set; } = null;
        public Font Font { get; set; } = null;

        public Padding GlyphMargin { get; set; }
        public Padding GlyphPadding { get; set; }

        public int GlyphHeight { get; set; } = 50;
        public int CanvasWidth { get; set; } = 1024;
        public int CanvasHeight { get; set; } = 512;
        public bool ShowDebugBoxes { get; set; } = false;

        public void Generate(List<ushort> characters)
        {
            if (Font == null)
                throw new InvalidOperationException("No font was specified.");

            if (Adapter == null)
                throw new InvalidOperationException("No font adapter was provided.");

            if (!(Adapter is IAddCharacters))
                throw new InvalidOperationException("The font adapter provided is not capable of adding characters.");

            if (characters == null || characters.Count == 0)
                throw new InvalidOperationException("No characters were specified.");

            // Clear out the existing characters
            Adapter.Characters = new List<FontCharacter>();
            Adapter.Textures = new List<Bitmap>();

            var img = new Bitmap(CanvasWidth, CanvasHeight);
            Adapter.Textures.Add(img);

            var gfx = Graphics.FromImage(img);
            gfx.SmoothingMode = SmoothingMode.None;
            gfx.InterpolationMode = InterpolationMode.Bicubic;
            gfx.PixelOffsetMode = PixelOffsetMode.Default;
            gfx.TextRenderingHint = TextRenderingHint.AntiAlias;

            var glyphPos = new Point(0, 0);
            var color = Color.FromArgb(180, 255, 0, 0);
            foreach (var character in characters.Distinct())
            {
                var c = (char)character;
                var cstr = c.ToString();

                // Get Bounds
                var size = Regex.IsMatch(cstr, @"\s") ? gfx.MeasureString(cstr, Font, new SizeF(1000, 1000), StringFormat.GenericDefault) : gfx.MeasureString(cstr, Font, new SizeF(1000, 1000), StringFormat.GenericTypographic);
                var charDim = new Size((int)Math.Ceiling(size.Width), GlyphHeight);
                var glyphDim = new Size(charDim.Width, charDim.Height);

                var cat = char.GetUnicodeCategory(c);
                if (cat == UnicodeCategory.OtherLetter || c == '　')
                    glyphDim.Width = glyphDim.Height;
                else
                    glyphDim.Width += GlyphPadding.Left + GlyphPadding.Right;

                // Margin
                glyphDim.Width += GlyphMargin.Left + GlyphMargin.Right;
                glyphDim.Height += GlyphMargin.Top + GlyphMargin.Bottom;

                // Line Change
                if (glyphPos.X + glyphDim.Width >= CanvasWidth)
                {
                    glyphPos.X = 0;
                    glyphPos.Y += glyphDim.Height;

                    if (glyphPos.Y + glyphDim.Height >= CanvasHeight)
                    {
                        glyphPos.Y = 0;
                        img = new Bitmap(CanvasWidth, CanvasHeight);
                        gfx = Graphics.FromImage(img);
                        gfx.SmoothingMode = SmoothingMode.None;
                        gfx.InterpolationMode = InterpolationMode.Bicubic;
                        gfx.PixelOffsetMode = PixelOffsetMode.Default;
                        gfx.TextRenderingHint = TextRenderingHint.AntiAlias;
                        Adapter.Textures.Add(img);
                    }
                }

                // Calculate Glyph Centering | Margin & Padding
                var charPos = new Point(glyphPos.X, glyphPos.Y);
                if (cat == UnicodeCategory.OtherLetter)
                    charPos.X += GlyphMargin.Left + (int)Math.Ceiling((float)(glyphDim.Width - GlyphMargin.Left - GlyphMargin.Right) / 2 - (float)charDim.Width / 2);
                else
                    charPos.X += GlyphMargin.Left + GlyphPadding.Left;

                charPos.Y += GlyphMargin.Top + GlyphPadding.Top;

                // Draw Character
                gfx.DrawString(cstr, Font, new SolidBrush(Color.White), new PointF(charPos.X, charPos.Y), StringFormat.GenericTypographic);

                if (ShowDebugBoxes)
                {
                    color = color == Color.FromArgb(180, 255, 0, 0) ? Color.FromArgb(180, 0, 0, 0) : Color.FromArgb(180, 255, 0, 0);

                    // Disable Padding
                    if (cat == UnicodeCategory.OtherLetter)
                    {
                        charPos.X = glyphPos.X;
                        charDim.Width = glyphDim.Width;
                    }

                    // Glyph Box
                    gfx.DrawRectangle(new Pen(Color.FromArgb(100, 0, 0, 0), 1), new Rectangle(glyphPos.X, glyphPos.Y, glyphDim.Width - 1, glyphDim.Height - 1));

                    // Character Box
                    gfx.DrawRectangle(new Pen(color, 1), new Rectangle(glyphPos.X + GlyphMargin.Left, glyphPos.Y + GlyphMargin.Top, glyphDim.Width - GlyphMargin.Left - GlyphMargin.Right - 1, glyphDim.Height - GlyphMargin.Top - GlyphMargin.Bottom - 1));
                }

                // Add Character
                if (Adapter is IAddCharacters add)
                {
                    var fc = add.NewCharacter();
                    fc.Character = character;
                    fc.TextureID = Adapter.Textures.IndexOf(img);
                    fc.GlyphX = glyphPos.X;
                    fc.GlyphY = glyphPos.Y;
                    fc.GlyphWidth = glyphDim.Width;
                    fc.GlyphHeight = glyphDim.Height;
                    add.AddCharacter(fc);
                }

                // Next X
                glyphPos.X += glyphDim.Width;
            }
        }

        public Bitmap Preview(char c)
        {
            var img = new Bitmap(CanvasWidth, CanvasHeight);

            var gfx = Graphics.FromImage(img);
            gfx.SmoothingMode = SmoothingMode.None;
            gfx.InterpolationMode = InterpolationMode.Bicubic;
            gfx.PixelOffsetMode = PixelOffsetMode.Default;
            gfx.TextRenderingHint = TextRenderingHint.AntiAlias;

            var glyphPos = new Point(0, 0);
            var color = Color.Red;
            var cstr = c.ToString();

            // Get Bounds
            var size = Regex.IsMatch(cstr, @"\s") ? gfx.MeasureString(cstr, Font, new SizeF(1000, 1000), StringFormat.GenericDefault) : gfx.MeasureString(cstr, Font, new SizeF(1000, 1000), StringFormat.GenericTypographic);
            var charDim = new Size((int)Math.Ceiling(size.Width), GlyphHeight);
            var glyphDim = new Size(charDim.Width, charDim.Height);

            // Adjust Glyph Dimensions
            var cat = char.GetUnicodeCategory(c);
            if (cat == UnicodeCategory.OtherLetter || c == '　')
                glyphDim.Width = glyphDim.Height;
            else
                glyphDim.Width += GlyphPadding.Left + GlyphPadding.Right;

            // Margin
            glyphDim.Width += GlyphMargin.Left + GlyphMargin.Right;
            glyphDim.Height += GlyphMargin.Top + GlyphMargin.Bottom;

            // Reset the Bitmap
            img = new Bitmap(glyphDim.Width, glyphDim.Height);
            gfx = Graphics.FromImage(img);
            gfx.SmoothingMode = SmoothingMode.None;
            gfx.InterpolationMode = InterpolationMode.Bicubic;
            gfx.PixelOffsetMode = PixelOffsetMode.Default;
            gfx.TextRenderingHint = TextRenderingHint.AntiAlias;

            // Calculate Glyph Centering | Margin & Padding
            var charPos = new Point(glyphPos.X, glyphPos.Y);
            if (cat == UnicodeCategory.OtherLetter)
                charPos.X += GlyphMargin.Left + (int)Math.Ceiling((float)(glyphDim.Width - GlyphMargin.Left - GlyphMargin.Right) / 2 - (float)charDim.Width / 2);
            else
                charPos.X += GlyphMargin.Left + GlyphPadding.Left;

            charPos.Y += GlyphMargin.Top + GlyphPadding.Top;

            // Draw Character
            gfx.DrawString(cstr, Font, new SolidBrush(Color.White), new PointF(charPos.X, charPos.Y), StringFormat.GenericTypographic);

            // Disable Padding
            if (cat == UnicodeCategory.OtherLetter)
            {
                charPos.X = glyphPos.X;
                charDim.Width = glyphDim.Width;
            }

            // Character Box
            gfx.DrawRectangle(new Pen(Color.FromArgb(200, 255, 0, 0), 1), new Rectangle(charPos.X - GlyphPadding.Left, charPos.Y - GlyphPadding.Top, charDim.Width + GlyphPadding.Left + GlyphPadding.Right - 1, charDim.Height - 1));

            // Glyph Box
            gfx.DrawRectangle(new Pen(Color.FromArgb(200, 255, 255, 0), 1), new Rectangle(glyphPos.X, glyphPos.Y, glyphDim.Width - 1, glyphDim.Height - 1));

            return img;
        }
    }
}
