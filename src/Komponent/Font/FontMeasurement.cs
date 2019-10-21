using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace Komponent.Font
{
    public static class FontMeasurement
    {
        public static unsafe List<WhiteSpaceAdjustment> MeasureWhiteSpace(List<Bitmap> glyphs)
        {
            var result = new List<WhiteSpaceAdjustment>(glyphs.Count);

            foreach (var glyph in glyphs)
            {
                var bitmapData = glyph.LockBits(new Rectangle(0, 0, glyph.Width, glyph.Height), ImageLockMode.ReadOnly,
                    PixelFormat.Format32bppArgb);

                var dataPtr = (byte*)bitmapData.Scan0;
                var top = MeasureWhiteSpaceSide(0, dataPtr, glyph.Width, glyph.Height);
                var left = MeasureWhiteSpaceSide(1, dataPtr, glyph.Width, glyph.Height);
                var bottom = MeasureWhiteSpaceSide(2, dataPtr, glyph.Width, glyph.Height);
                var right = MeasureWhiteSpaceSide(3, dataPtr, glyph.Width, glyph.Height);

                glyph.UnlockBits(bitmapData);

                var adjustment = new WhiteSpaceAdjustment(glyph,
                    new Point(top, left),
                    new Size(glyph.Width - left - right, glyph.Height - top - bottom));
                result.Add(adjustment);
            }

            return result;
        }

        private static unsafe int MeasureWhiteSpaceSide(int mode, byte* data, int width, int height)
        {
            switch (mode)
            {
                // Top
                case 0:
                    for (var h = 0; h < height; h++)
                        for (var w = 0; w < width * 4; w += 4)
                            if (data[h * height + w] != 0)
                                return h;
                    return height;

                // Left
                case 1:
                    for (var w = 0; w < width * 4; w += 4)
                        for (var h = 0; h < height; h++)
                            if (data[h * height + w] != 0)
                                return w >> 2;
                    return width;

                // Bottom
                case 2:
                    for (var h = height - 1; h >= 0; h--)
                        for (var w = 0; w < width * 4; w += 4)
                            if (data[h * height + w] != 0)
                                return height - h - 1;
                    return height;

                // Right
                case 3:
                    for (var w = width * 4 - 4; w >= 0; w -= 4)
                        for (var h = 0; h < height; h++)
                            if (data[h * height + w] != 0)
                                return width - (w >> 2) - 1;
                    return width;

                default:
                    return -1;
            }
        }
    }
}
