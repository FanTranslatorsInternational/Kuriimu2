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
using Size = SixLabors.ImageSharp.Size;

namespace Kaligraphy.Rendering;

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
        return GetOrDefault(codePoint, SixLabors.ImageSharp.Color.White);
    }

    public CharacterInfo? GetOrDefault(ushort codePoint, SixLabors.ImageSharp.Color textColor)
    {
        if (_glyphs.TryGetValue(codePoint, out CharacterInfo? cachedInfo))
            return cachedInfo;

        System.Drawing.SizeF glyphSize = MeasureCharacter((char)codePoint, _font);
        if (glyphSize.Width <= 0 || glyphSize.Height <= 0)
        {
            return _glyphs[codePoint] = new CharacterInfo
            {
                CodePoint = (char)codePoint,
                GlyphPosition = Point.Empty,
                BoundingBox = Size.Empty,
                Glyph = null
            };
        }

        var glyphImage = new Bitmap((int)Math.Ceiling(glyphSize.Width), (int)Math.Ceiling(glyphSize.Height));
        using Graphics gfx = Graphics.FromImage(glyphImage);

        gfx.SmoothingMode = SmoothingMode.HighQuality;
        gfx.InterpolationMode = InterpolationMode.HighQualityBicubic;
        gfx.PixelOffsetMode = PixelOffsetMode.None;
        gfx.TextRenderingHint = TextRenderingHint.AntiAlias;

        var pixelColor = textColor.ToPixel<Rgba32>();
        Color glyphColor = Color.FromArgb(pixelColor.A, pixelColor.R, pixelColor.G, pixelColor.B);
        gfx.DrawString($"{(char)codePoint}", _font, new SolidBrush(glyphColor), PointF.Empty, StringFormat.GenericTypographic);

        Image<Rgba32> glyph = ConvertSystemDrawing(glyphImage);
        GlyphDescriptionData glyphDescription = WhiteSpaceMeasurer.MeasureWhiteSpace(glyph);

        if (glyphDescription.Size is { Width: > 0, Height: > 0 })
            glyph = glyph.Clone(context => context.Crop(new SixLabors.ImageSharp.Rectangle(glyphDescription.Position, glyphDescription.Size)));

        float glyphY = GetBaseline(_font, gfx.DpiY) - GetAscent(_font, gfx.DpiY) + 0.475f;

        return _glyphs[codePoint] = new CharacterInfo
        {
            CodePoint = (char)codePoint,
            GlyphPosition = new Point(glyphDescription.Position.X, (int)(glyphDescription.Position.Y + glyphY)),
            BoundingBox = new Size((int)glyphSize.Width, (int)glyphSize.Height),
            Glyph = glyph
        };
    }

    public int GetMaxHeight() => (int)_font.GetHeight();

    private static System.Drawing.SizeF MeasureCharacter(char character, Font font)
    {
        using Graphics gfx = Graphics.FromHwnd(nint.Zero);

        StringFormat fmt = StringFormat.GenericTypographic;
        fmt.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;

        return gfx.MeasureString($"{character}", font, PointF.Empty, fmt);
    }

    private static float GetBaseline(Font font, float dpiY)
    {
        return font.GetHeight() - GetDescent(font, dpiY);
    }

    private static float GetAscent(Font font, float dpiY)
    {
        return dpiY / 72f *
               (font.SizeInPoints / font.FontFamily.GetEmHeight(font.Style) *
                font.FontFamily.GetCellAscent(font.Style));
    }

    private static float GetDescent(Font font, float dpiY)
    {
        return dpiY / 72f *
               (font.SizeInPoints / font.FontFamily.GetEmHeight(font.Style) *
                font.FontFamily.GetCellDescent(font.Style));
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