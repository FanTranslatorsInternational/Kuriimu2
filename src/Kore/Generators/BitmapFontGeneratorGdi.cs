using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
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

        public int GlyphHeight { get; set; } = 36;
        public float Baseline { get; set; } = 30;
        public int CanvasWidth { get; set; } = 512;
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

            var baseline = Baseline + +GlyphMargin.Top;
            var baselineOffsetPixels = Baseline - gfx.DpiY / 72f * (Font.SizeInPoints / Font.FontFamily.GetEmHeight(Font.Style) * Font.FontFamily.GetCellAscent(Font.Style));

            var imagePos = new Point(0, 0);
            var color = Color.FromArgb(180, 255, 0, 0);
            foreach (var character in characters.Distinct())
            {
                var c = (char)character;
                var cstr = c.ToString();

                var measuredWidth = Regex.IsMatch(cstr, @"\s") ? gfx.MeasureString(cstr, Font, new SizeF(1000, 1000), StringFormat.GenericDefault).Width : gfx.MeasureString(cstr, Font, new SizeF(1000, 1000), StringFormat.GenericTypographic).Width;

                var charPos = new Point(0, 0);
                var charDim = new Size((int)Math.Ceiling(measuredWidth), GlyphHeight);

                // New Line/Page
                if (imagePos.X + charDim.Width + GlyphMargin.Left + GlyphMargin.Right + GlyphPadding.Left + GlyphPadding.Right >= CanvasWidth)
                {
                    imagePos.X = 0;
                    imagePos.Y += charDim.Height + GlyphMargin.Top + GlyphMargin.Bottom;

                    if (imagePos.Y + charDim.Height + GlyphMargin.Top + GlyphMargin.Bottom >= CanvasHeight)
                    {
                        imagePos.Y = 0;
                        img = new Bitmap(CanvasWidth, CanvasHeight);
                        gfx = Graphics.FromImage(img);
                        gfx.SmoothingMode = SmoothingMode.None;
                        gfx.InterpolationMode = InterpolationMode.Bicubic;
                        gfx.PixelOffsetMode = PixelOffsetMode.Default;
                        gfx.TextRenderingHint = TextRenderingHint.AntiAlias;
                        Adapter.Textures.Add(img);
                    }
                }

                var glyphPos = new Point(imagePos.X + GlyphMargin.Left, imagePos.Y + GlyphMargin.Top);
                var glyphDim = new Size(charDim.Width + GlyphPadding.Left + GlyphPadding.Right, charDim.Height);

                var imageDim = new Size(glyphDim.Width + GlyphMargin.Left + GlyphMargin.Right, glyphDim.Height + GlyphMargin.Top + GlyphMargin.Bottom);

                //var cat = char.GetUnicodeCategory(c);
                //if (cat == UnicodeCategory.OtherLetter || c == '　')
                //    glyphDim.Width = glyphDim.Height;
                //else
                //    glyphDim.Width += GlyphPadding.Left + GlyphPadding.Right;

                // Calculate Glyph Centering | Margin & Padding
                //var charPos = new Point(glyphPos.X, glyphPos.Y);
                //if (cat == UnicodeCategory.OtherLetter)
                //    charPos.X += GlyphMargin.Left + (int)Math.Ceiling((float)(glyphDim.Width - GlyphMargin.Left - GlyphMargin.Right) / 2 - (float)charDim.Width / 2);
                //else
                //    charPos.X += GlyphMargin.Left + GlyphPadding.Left;

                charPos.X = imagePos.X + GlyphMargin.Left + GlyphPadding.Left;
                charPos.Y = imagePos.Y + GlyphMargin.Top;

                if (ShowDebugBoxes)
                {
                    color = color == Color.FromArgb(180, 255, 0, 0) ? Color.FromArgb(180, 0, 0, 0) : Color.FromArgb(180, 255, 0, 0);

                    //// Disable Padding
                    //if (cat == UnicodeCategory.OtherLetter)
                    //{
                    //    charPos.X = glyphPos.X;
                    //    charDim.Width = glyphDim.Width;
                    //}

                    // Image Box
                    gfx.DrawRectangle(new Pen(Color.FromArgb(80, 0, 0, 0), 1), new Rectangle(imagePos.X, imagePos.Y, imageDim.Width - 1, imageDim.Height - 1));

                    // Baseline
                    gfx.DrawLine(new Pen(Color.FromArgb(100, 255, 255, 255)), new PointF(imagePos.X, imagePos.Y + baseline), new PointF(imagePos.X + imageDim.Width - 1, imagePos.Y + baseline));

                    // Character Box
                    gfx.DrawRectangle(new Pen(Color.FromArgb(127, 255, 255, 0), 1), new Rectangle(charPos.X, glyphPos.Y, charDim.Width - 1, charDim.Height - 1));

                    // Glyph Box
                    gfx.DrawRectangle(new Pen(Color.FromArgb(127, 255, 0, 0), 1), new Rectangle(glyphPos.X, glyphPos.Y, glyphDim.Width - 1, glyphDim.Height - 1));
                }

                // Draw Character
                gfx.DrawString(cstr, Font, new SolidBrush(Color.White), new PointF(charPos.X, charPos.Y + baselineOffsetPixels + 0.475f), StringFormat.GenericTypographic);

                // Add Character
                if (Adapter is IAddCharacters add)
                {
                    var fc = add.NewCharacter();
                    fc.Character = character;
                    fc.TextureID = Adapter.Textures.IndexOf(img);
                    fc.GlyphX = charPos.X;
                    fc.GlyphY = charPos.Y;
                    fc.GlyphWidth = charDim.Width;
                    fc.GlyphHeight = charDim.Height;
                    add.AddCharacter(fc);
                }

                // Next
                imagePos.X += imageDim.Width;
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

            var baseline = Baseline + +GlyphMargin.Top;
            var baselineOffsetPixels = Baseline - gfx.DpiY / 72f * (Font.SizeInPoints / Font.FontFamily.GetEmHeight(Font.Style) * Font.FontFamily.GetCellAscent(Font.Style));

            var cstr = c.ToString();

            var measuredWidth = Regex.IsMatch(cstr, @"\s") ? gfx.MeasureString(cstr, Font, new SizeF(1000, 1000), StringFormat.GenericDefault).Width : gfx.MeasureString(cstr, Font, new SizeF(1000, 1000), StringFormat.GenericTypographic).Width;

            var charPos = new Point(0, 0);
            var charDim = new Size((int)Math.Ceiling(measuredWidth), GlyphHeight);

            var glyphPos = new Point(GlyphMargin.Left, GlyphMargin.Top);
            var glyphDim = new Size(charDim.Width + GlyphPadding.Left + GlyphPadding.Right, charDim.Height);

            var imageDim = new Size(glyphDim.Width + GlyphMargin.Left + GlyphMargin.Right, glyphDim.Height + GlyphMargin.Top + GlyphMargin.Bottom);
            if (imageDim.Width == 0 || imageDim.Height == 0) return null;

            // Adjust Glyph Dimensions
            //var cat = char.GetUnicodeCategory(c);
            //if (cat == UnicodeCategory.OtherLetter || c == '　')
            //    glyphDim.Width = glyphDim.Height;
            //else
            //    glyphDim.Width += GlyphPadding.Left + GlyphPadding.Right;

            // Reset the Bitmap
            img = new Bitmap(imageDim.Width, imageDim.Height);
            gfx = Graphics.FromImage(img);
            gfx.SmoothingMode = SmoothingMode.None;
            gfx.InterpolationMode = InterpolationMode.Bicubic;
            gfx.PixelOffsetMode = PixelOffsetMode.Default;
            gfx.TextRenderingHint = TextRenderingHint.AntiAlias;

            // Calculate Glyph Centering | Margin & Padding
            //if (cat == UnicodeCategory.OtherLetter)
            //    charPos.X += GlyphMargin.Left + (int)Math.Ceiling((float)(glyphDim.Width - GlyphMargin.Left - GlyphMargin.Right) / 2 - (float)charDim.Width / 2);
            //else

            charPos.X += GlyphMargin.Left + GlyphPadding.Left;
            charPos.Y += GlyphMargin.Top; /* + GlyphPadding.Top;*/

            //// Disable Padding
            //if (cat == UnicodeCategory.OtherLetter)
            //{
            //    charPos.X = glyphPos.X;
            //    charDim.Width = glyphDim.Width;
            //}

            // Image Box
            gfx.DrawRectangle(new Pen(Color.FromArgb(80, 0, 0, 0), 1), new Rectangle(0, 0, imageDim.Width - 1, imageDim.Height - 1));

            // Baseline
            gfx.DrawLine(new Pen(Color.FromArgb(100, 255, 255, 255)), new PointF(0, baseline), new PointF(imageDim.Width, baseline));

            // Character Box
            gfx.DrawRectangle(new Pen(Color.FromArgb(127, 255, 255, 0), 1), new Rectangle(charPos.X, glyphPos.Y, charDim.Width - 1, charDim.Height - 1));

            // Glyph Box
            gfx.DrawRectangle(new Pen(Color.FromArgb(127, 255, 0, 0), 1), new Rectangle(glyphPos.X, glyphPos.Y, glyphDim.Width - 1, glyphDim.Height - 1));

            // Draw Character
            gfx.DrawString(cstr, Font, new SolidBrush(Color.White), new PointF(charPos.X, charPos.Y + baselineOffsetPixels + 0.475f), StringFormat.GenericTypographic);

            return img;
        }
    }

    [XmlRoot("profile")]
    public class BitmapFontGeneratorGdiProfile
    {
        [XmlElement("fontFamily")]
        public string FontFamily { get; set; } = "Arial";

        [XmlElement("fontSize")]
        public float FontSize { get; set; } = 24;

        [XmlElement("baseline")]
        public float Baseline { get; set; } = 30;

        [XmlElement("glyphHeight")]
        public int GlyphHeight { get; set; } = 36;

        [XmlElement("bold")]
        public bool Bold { get; set; }

        [XmlElement("italic")]
        public bool Italic { get; set; }

        [XmlElement("padding")]
        public Padding GlyphMargin { get; set; }

        [XmlElement("margin")]
        public Padding GlyphPadding { get; set; }

        [XmlElement("canvasWidth")]
        public int CanvasWidth { get; set; } = 512;

        [XmlElement("canvasHeight")]
        public int CanvasHeight { get; set; } = 512;

        [XmlElement("showDebugBoxes")]
        public bool ShowDebugBoxes { get; set; }

        public static BitmapFontGeneratorGdiProfile Load(string filename)
        {
            var xmlSettings = new XmlReaderSettings { CheckCharacters = false };

            using (var fs = File.OpenRead(filename))
            {
                return (BitmapFontGeneratorGdiProfile)new XmlSerializer(typeof(BitmapFontGeneratorGdiProfile)).Deserialize(XmlReader.Create(fs, xmlSettings));
            }
        }

        public void Save(string filename)
        {
            var xmlSettings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                NewLineOnAttributes = false,
                NewLineHandling = NewLineHandling.Entitize,
                IndentChars = "	",
                CheckCharacters = false
            };

            using (var xmlIO = new StreamWriter(filename, false, xmlSettings.Encoding))
            {
                var serializer = new XmlSerializer(typeof(BitmapFontGeneratorGdiProfile));
                var namespaces = new XmlSerializerNamespaces();
                namespaces.Add(string.Empty, string.Empty);
                serializer.Serialize(XmlWriter.Create(xmlIO, xmlSettings), this, namespaces);
            }
        }
    }
}
