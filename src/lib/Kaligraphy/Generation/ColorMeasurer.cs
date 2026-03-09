using Kaligraphy.Contract.DataClasses;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Kaligraphy.Generation;

/// <summary>
/// Static methods for measuring colored border space.
/// </summary>
public class ColorMeasurer
{
    /// <summary>
    /// Measure the colored border of an image.
    /// </summary>
    /// <param name="image">The image to measure.</param>
    /// <param name="color">The color of the border.</param>
    /// <returns>The measured border.</returns>
    public static BorderSpaceData MeasureColor(Image<Rgba32> image, Rgba32 color)
    {
        return MeasureColor(image, new Rectangle(0, 0, image.Width, image.Height), color);
    }

    /// <summary>
    /// Measure the colored border of an image.
    /// </summary>
    /// <param name="image">The image to measure on.</param>
    /// <param name="cropRect">The area to measure in.</param>
    /// <param name="color">The color of the border.</param>
    /// <returns>The measured border.</returns>
    public static BorderSpaceData MeasureColor(Image<Rgba32> image, Rectangle cropRect, Rgba32 color)
    {
        int top = MeasureColorTop(image, cropRect, color);
        int left = MeasureColorLeft(image, cropRect, color);

        if (top >= cropRect.Bottom || left >= cropRect.Right)
        {
            return new BorderSpaceData
            {
                Position = new Point(left, top),
                Size = Size.Empty
            };
        }

        int bottom = MeasureColorBottom(image, cropRect, color);
        int right = MeasureColorRight(image, cropRect, color);

        return new BorderSpaceData
        {
            Position = new Point(left, top),
            Size = new Size(right - left, bottom - top)
        };
    }

    private static int MeasureColorTop(Image<Rgba32> glyph, Rectangle cropRect, Rgba32 color)
    {
        for (int y = cropRect.Top; y < cropRect.Bottom; y++)
        for (int x = cropRect.Left; x < cropRect.Right; x++)
            if (glyph[x, y] != color)
                return y;

        return cropRect.Bottom;
    }

    private static int MeasureColorLeft(Image<Rgba32> glyph, Rectangle cropRect, Rgba32 color)
    {
        for (int x = cropRect.Left; x < cropRect.Right; x++)
        for (int y = cropRect.Top; y < cropRect.Bottom; y++)
            if (glyph[x, y] != color)
                return x;

        return cropRect.Right;
    }

    private static int MeasureColorBottom(Image<Rgba32> glyph, Rectangle cropRect, Rgba32 color)
    {
        for (int y = cropRect.Bottom - 1; y >= cropRect.Top; y--)
        for (int x = cropRect.Left; x < cropRect.Right; x++)
            if (glyph[x, y] != color)
                return y + 1;

        return cropRect.Top;
    }

    private static int MeasureColorRight(Image<Rgba32> glyph, Rectangle cropRect, Rgba32 color)
    {
        for (int x = cropRect.Right - 1; x >= cropRect.Left; x--)
        for (int y = cropRect.Top; y < cropRect.Bottom; y++)
            if (glyph[x, y] != color)
                return x + 1;

        return cropRect.Left;
    }
}