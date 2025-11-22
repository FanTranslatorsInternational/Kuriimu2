using Kaligraphy.Contract.DataClasses;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Kaligraphy.Generation;

/// <summary>
/// Static methods for font measurements.
/// </summary>
public static class WhiteSpaceMeasurer
{
    /// <summary>
    /// Measure the whitespace of a glyph.
    /// </summary>
    /// <param name="glyph">The glyph to measure.</param>
    /// <returns>The measured whitespace.</returns>
    public static GlyphDescriptionData MeasureWhiteSpace(Image<Rgba32> glyph)
    {
        return MeasureWhiteSpace(glyph, new Rectangle(0, 0, glyph.Width, glyph.Height));
    }

    /// <summary>
    /// Measure the whitespace of a glyph.
    /// </summary>
    /// <param name="image">The image to measure on.</param>
    /// <param name="cropRect">The area to measure in.</param>
    /// <returns>The measured whitespace.</returns>
    public static GlyphDescriptionData MeasureWhiteSpace(Image<Rgba32> image, Rectangle cropRect)
    {
        int top = MeasureWhiteSpaceTop(image, cropRect);
        int left = MeasureWhiteSpaceLeft(image, cropRect);

        if (top >= cropRect.Bottom || left >= cropRect.Right)
        {
            return new GlyphDescriptionData
            {
                Position = new Point(left, top),
                Size = Size.Empty
            };
        }

        int bottom = MeasureWhiteSpaceBottom(image, cropRect);
        int right = MeasureWhiteSpaceRight(image, cropRect);

        return new GlyphDescriptionData
        {
            Position = new Point(left, top),
            Size = new Size(right - left, bottom - top)
        };
    }

    private static int MeasureWhiteSpaceTop(Image<Rgba32> glyph, Rectangle cropRect)
    {
        for (int y = cropRect.Top; y < cropRect.Bottom; y++)
        for (int x = cropRect.Left; x < cropRect.Right; x++)
            if (glyph[x, y].A > 0)
                return y;

        return cropRect.Bottom;
    }

    private static int MeasureWhiteSpaceLeft(Image<Rgba32> glyph, Rectangle cropRect)
    {
        for (int x = cropRect.Left; x < cropRect.Right; x++)
        for (int y = cropRect.Top; y < cropRect.Bottom; y++)
            if (glyph[x, y].A > 0)
                return x;

        return cropRect.Right;
    }

    private static int MeasureWhiteSpaceBottom(Image<Rgba32> glyph, Rectangle cropRect)
    {
        for (int y = cropRect.Bottom - 1; y >= cropRect.Top; y--)
        for (int x = cropRect.Left; x < cropRect.Right; x++)
            if (glyph[x, y].A > 0)
                return y + 1;

        return cropRect.Top;
    }

    private static int MeasureWhiteSpaceRight(Image<Rgba32> glyph, Rectangle cropRect)
    {
        for (int x = cropRect.Right - 1; x >= cropRect.Left; x--)
        for (int y = cropRect.Top; y < cropRect.Bottom; y++)
            if (glyph[x, y].A > 0)
                return x + 1;

        return cropRect.Left;
    }
}