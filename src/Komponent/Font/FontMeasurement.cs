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

                var top = MeasureWhiteSpaceSide(0, bitmapData);
                var left = MeasureWhiteSpaceSide(1, bitmapData);
                var bottom = MeasureWhiteSpaceSide(2, bitmapData);
                var right = MeasureWhiteSpaceSide(3, bitmapData);

                glyph.UnlockBits(bitmapData);

                var adjustment = new WhiteSpaceAdjustment(
                    new Point(left, top),
                    new Size(glyph.Width - left - right, glyph.Height - top - bottom));
                yield return new AdjustedGlyph(glyph, adjustment);
            }
        }

        private static unsafe int MeasureWhiteSpaceSide(int mode, BitmapData data)
        {
            var dataPtr = (byte*)data.Scan0;
            if (dataPtr == null)
                return -1;

            var widthInBytes = data.Width * 4;
            var dataLength = data.Height * widthInBytes;
            switch (mode)
            {
                // Top
                case 0:
                    for (var h = 0; h < dataLength; h += widthInBytes)
                        for (var w = 0; w < widthInBytes; w += 4)
                            if (dataPtr[h + w] != 0)
                                return h / widthInBytes;
                    return data.Height;

                // Left
                case 1:
                    for (var w = 0; w < widthInBytes; w += 4)
                        for (var h = 0; h < dataLength; h += widthInBytes)
                            if (dataPtr[h + w] != 0)
                                return w >> 2;
                    return data.Width;

                // Bottom
                case 2:
                    for (var h = dataLength - widthInBytes; h >= 0; h -= widthInBytes)
                        for (var w = 0; w < widthInBytes; w += 4)
                            if (dataPtr[h + w] != 0)
                                return data.Height - h / widthInBytes - 1;
                    return 0;

                // Right
                case 3:
                    for (var w = widthInBytes - 4; w >= 0; w -= 4)
                        for (var h = 0; h < dataLength; h += widthInBytes)
                            if (dataPtr[h + w] != 0)
                                return data.Width - (w >> 2) - 1;
                    return 0;

                default:
                    return -1;
            }
        }
    }
}
