using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace Komponent.Font
{
    /// <summary>
    /// Static methods for font measurements.
    /// </summary>
    public static class FontMeasurement
    {
        /// <summary>
        /// Measure the whitespace of glyphs.
        /// </summary>
        /// <param name="glyphs">The glyphs to measure.</param>
        /// <returns>The measured glyphs.</returns>
        public static IEnumerable<AdjustedGlyph> MeasureWhiteSpace(IEnumerable<Bitmap> glyphs)
        {
            foreach (var glyph in glyphs)
            {
                var bitmapData = glyph.LockBits(new Rectangle(0, 0, glyph.Width, glyph.Height), ImageLockMode.ReadOnly,
                    PixelFormat.Format32bppArgb);

                var top = MeasureWhiteSpaceSide(0, bitmapData, glyph.Width, glyph.Height);
                var left = MeasureWhiteSpaceSide(1, bitmapData, glyph.Width, glyph.Height);
                var bottom = MeasureWhiteSpaceSide(2, bitmapData, glyph.Width, glyph.Height);
                var right = MeasureWhiteSpaceSide(3, bitmapData, glyph.Width, glyph.Height);

                glyph.UnlockBits(bitmapData);

                var adjustment = new WhiteSpaceAdjustment(
                    new Point(left, top),
                    new Size(glyph.Width - left - right, glyph.Height - top - bottom));
                yield return new AdjustedGlyph(glyph, adjustment);
            }
        }

        private static unsafe int MeasureWhiteSpaceSide(int mode, BitmapData data, int width, int height)
        {
            var dataPtr = (byte*)data.Scan0;
            if (dataPtr == null)
                return -1;

            switch (mode)
            {
                // Top
                case 0:
                    for (var h = 0; h < height; h++)
                        for (var w = 0; w < width * 4; w += 4)
                            if (dataPtr[h * height * 4 + w] != 0)
                                return h;
                    return height;

                // Left
                case 1:
                    for (var w = 0; w < width * 4; w += 4)
                        for (var h = 0; h < height; h++)
                            if (dataPtr[h * height * 4 + w] != 0)
                                return w >> 2;
                    return width;

                // Bottom
                case 2:
                    for (var h = height - 1; h >= 0; h--)
                        for (var w = 0; w < width * 4; w += 4)
                            if (dataPtr[h * height * 4 + w] != 0)
                                return height - h - 1;
                    return height;

                // Right
                case 3:
                    for (var w = width * 4 - 4; w >= 0; w -= 4)
                        for (var h = 0; h < height; h++)
                            if (dataPtr[h * height * 4 + w] != 0)
                                return width - (w >> 2) - 1;
                    return width;

                default:
                    return -1;
            }
        }
    }
}
