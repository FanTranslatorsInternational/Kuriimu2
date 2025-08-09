using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using Kaligraphy.Contract.DataClasses;
using Kaligraphy.Contract.DataClasses.Rendering;
using Kaligraphy.Contract.Rendering;
using Kaligraphy.Generation;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = System.Drawing.Color;
using Point = SixLabors.ImageSharp.Point;
using PointF = System.Drawing.PointF;
using RectangleF = System.Drawing.RectangleF;
using Size = SixLabors.ImageSharp.Size;
using SizeF = System.Drawing.SizeF;

namespace Kaligraphy.Rendering
{
    public class SystemFontGlyphProvider : IGlyphProvider
    {
        private readonly Font _font;
        private readonly Dictionary<ushort, CharacterInfo> _glyphs = [];

        public SystemFontGlyphProvider(Font font)
        {
            _font = font;
        }

        public CharacterInfo? GetOrDefault(ushort codePoint)
        {
            if (_glyphs.TryGetValue(codePoint, out CharacterInfo? cachedInfo))
                return cachedInfo;

            RectangleF glyphSize = MeasureCharacter((char)codePoint, _font);

            var glyphImage = new Bitmap((int)glyphSize.Width, (int)glyphSize.Height);
            using Graphics gfx = Graphics.FromImage(glyphImage);

            gfx.SmoothingMode = SmoothingMode.HighQuality;
            gfx.InterpolationMode = InterpolationMode.HighQualityBicubic;
            gfx.PixelOffsetMode = PixelOffsetMode.None;
            gfx.TextRenderingHint = TextRenderingHint.AntiAlias;

            gfx.DrawString($"{(char)codePoint}", _font, new SolidBrush(Color.White), PointF.Empty, StringFormat.GenericTypographic);

            Image<Rgba32> glyph = ConvertSystemDrawing(glyphImage);
            GlyphDescriptionData glyphDescription = WhiteSpaceMeasurer.MeasureWhiteSpace(glyph);

            if (glyphDescription.Size is { Width: > 0, Height: > 0 })
                glyph = glyph.Clone(context => context.Crop(new SixLabors.ImageSharp.Rectangle(glyphDescription.Position, glyphDescription.Size)));

            return _glyphs[codePoint] = new CharacterInfo
            {
                CodePoint = (char)codePoint,
                GlyphPosition = new Point((int)glyphSize.Left, (int)glyphSize.Top),
                BoundingBox = new Size((int)glyphSize.Width, (int)glyphSize.Height),
                Glyph = glyph
            };
        }

        public int GetMaxHeight() => (int)_font.GetHeight();

        private static RectangleF MeasureCharacter(char character, Font font)
        {
            Graphics gfx = Graphics.FromHwnd(nint.Zero);

            var layout = new RectangleF(0, 0, 100, font.GetHeight());
            Region[] regions = gfx.MeasureCharacterRanges($"{character}", font, layout, StringFormat.GenericTypographic);

            return regions[0].GetBounds(gfx);
        }

        private static Image<Rgba32> ConvertSystemDrawing(Bitmap bitmap)
        {
            var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);

            ms.Position = 0;
            Image<Rgba32> glyph = SixLabors.ImageSharp.Image.Load<Rgba32>(ms);

            return glyph;
        }
    }
}
