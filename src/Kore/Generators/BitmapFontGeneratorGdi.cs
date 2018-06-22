using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Linq;
using Kontract.Interfaces;

namespace Kore.Generators
{
    public class BitmapFontGeneratorGdi
    {
        // Input
        public IFontAdapter Adapter { get; set; } = null;
        public Font Font { get; set; } = null;

        public int GlyphHeight { get; set; } = 50;
        public int GlyphLeftPadding { get; set; } = 0;
        public int GlyphRightPadding { get; set; } = 0;
        public int GlyphTopPadding { get; set; } = 0;

        public int MaxCanvasWidth { get; set; } = 1024;
        public int MaxCanvasHeight { get; set; } = 512;

        public bool Debug { get; set; } = false;

        public BitmapFontGeneratorGdi()
        {

        }

        public BitmapFontGeneratorGdi(int maxCanvasWidth, int maxCanvasHeight)
        {
            MaxCanvasWidth = maxCanvasWidth;
            MaxCanvasHeight = maxCanvasHeight;
        }

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

            var img = new Bitmap(MaxCanvasWidth, MaxCanvasHeight);
            Adapter.Textures.Add(img);

            var gfx = Graphics.FromImage(img);
            gfx.SmoothingMode = SmoothingMode.None;
            gfx.InterpolationMode = InterpolationMode.Bicubic;
            gfx.PixelOffsetMode = PixelOffsetMode.Default;
            gfx.TextRenderingHint = TextRenderingHint.AntiAlias;

            var cursor = new Point(0, 0);
            var color = Color.Red;
            foreach (var character in characters.Distinct())
            {
                var c = (char)character;
                var cstr = c.ToString();

                // Get character bounds
                var size = cstr == " " ? gfx.MeasureString(cstr, Font, new SizeF(1000, 1000), StringFormat.GenericDefault) : gfx.MeasureString(cstr, Font, new SizeF(1000, 1000), StringFormat.GenericTypographic);
                size.Width = (float)Math.Ceiling(size.Width);
                size.Height = GlyphHeight;

                var draw = new Size((int)size.Width, (int)size.Height);
                var glyphX = cursor.X;

                var cat = char.GetUnicodeCategory(c);
                if (cat == UnicodeCategory.OtherLetter)
                    draw.Width = draw.Height;
                else
                    draw.Width += GlyphLeftPadding + GlyphRightPadding;

                // Line Change
                if (cursor.X + draw.Width >= MaxCanvasWidth)
                {
                    cursor.X = 0;
                    cursor.Y += draw.Height;

                    if (cursor.Y + draw.Height >= MaxCanvasHeight)
                    {
                        cursor.Y = 0;
                        img = new Bitmap(MaxCanvasWidth, MaxCanvasHeight);
                        gfx = Graphics.FromImage(img);
                        gfx.SmoothingMode = SmoothingMode.None;
                        gfx.InterpolationMode = InterpolationMode.Bicubic;
                        gfx.PixelOffsetMode = PixelOffsetMode.Default;
                        gfx.TextRenderingHint = TextRenderingHint.AntiAlias;
                        Adapter.Textures.Add(img);
                    }
                }

                // Calculate Glyph Centering
                if (cat == UnicodeCategory.OtherLetter)
                    glyphX = cursor.X + (int)Math.Ceiling((float)draw.Width / 2 - size.Width / 2);
                else
                    glyphX = cursor.X + GlyphLeftPadding;

                // Draw Character
                gfx.DrawString(((char)character).ToString(), Font, new SolidBrush(Color.White), new PointF(glyphX, cursor.Y + GlyphTopPadding), StringFormat.GenericTypographic);

                if (Debug)
                {
                    color = color == Color.Red ? Color.Black : Color.Red;
                    gfx.DrawRectangle(new Pen(color, 1), new Rectangle(cursor.X, cursor.Y, draw.Width - 1, draw.Height - 1));
                }

                // Add Character
                if (Adapter is IAddCharacters add)
                {
                    var fc = add.NewCharacter();
                    fc.Character = character;
                    fc.TextureID = Adapter.Textures.IndexOf(img);
                    fc.GlyphX = cursor.X;
                    fc.GlyphY = cursor.Y;
                    fc.GlyphWidth = draw.Width;
                    fc.GlyphHeight = draw.Height;
                    add.AddCharacter(fc);
                }

                // Next X
                cursor.X += draw.Width;
            }
        }
    }
}
