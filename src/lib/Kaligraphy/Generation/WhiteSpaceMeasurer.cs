using Kaligraphy.Contract.DataClasses;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Kaligraphy.Generation
{
    /// <summary>
    /// Static methods for font measurements.
    /// </summary>
    public class WhiteSpaceMeasurer
    {
        /// <summary>
        /// Measure the whitespace of a glyph.
        /// </summary>
        /// <param name="glyph">The glyph to measure.</param>
        /// <returns>The measured whitespace.</returns>
        public GlyphDescriptionData MeasureWhiteSpace(Image<Rgba32> glyph)
        {
            int top = MeasureWhiteSpaceTop(glyph);
            int left = MeasureWhiteSpaceLeft(glyph);

            if (top >= glyph.Height || left >= glyph.Width)
            {
                return new GlyphDescriptionData
                {
                    Position = new Point(left, top),
                    Size = Size.Empty
                };
            }

            int bottom = MeasureWhiteSpaceBottom(glyph);
            int right = MeasureWhiteSpaceRight(glyph);

            return new GlyphDescriptionData
            {
                Position = new Point(left, top),
                Size = new Size(glyph.Width - left - right, glyph.Height - top - bottom)
            };
        }

        private int MeasureWhiteSpaceTop(Image<Rgba32> glyph)
        {
            for (var y = 0; y < glyph.Height; y++)
                for (var x = 0; x < glyph.Width; x++)
                    if ((Color)glyph[x, y] != Color.Transparent)
                        return y;

            return glyph.Height;
        }

        private int MeasureWhiteSpaceLeft(Image<Rgba32> glyph)
        {
            for (var x = 0; x < glyph.Width; x++)
                for (var y = 0; y < glyph.Height; y++)
                    if ((Color)glyph[x, y] != Color.Transparent)
                        return x;

            return glyph.Width;
        }

        private int MeasureWhiteSpaceBottom(Image<Rgba32> glyph)
        {
            for (int y = glyph.Height - 1; y >= 0; y--)
                for (var x = 0; x < glyph.Width; x++)
                    if ((Color)glyph[x, y] != Color.Transparent)
                        return glyph.Height - y - 1;

            return 0;
        }

        private int MeasureWhiteSpaceRight(Image<Rgba32> glyph)
        {
            for (var x = glyph.Width - 1; x >= 0; x--)
                for (var y = 0; y < glyph.Height; y++)
                    if ((Color)glyph[x, y] != Color.Transparent)
                        return glyph.Width - x - 1;

            return 0;
        }
    }
}
