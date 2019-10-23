using System.Drawing;

namespace Komponent.Font
{
    /// <summary>
    /// Combines a glyph with its optional white space adjustments.
    /// </summary>
    public class AdjustedGlyph
    {
        /// <summary>
        /// The adjustments made to reduce white space in a glyph.
        /// </summary>
        public WhiteSpaceAdjustment WhiteSpaceAdjustment { get; }

        /// <summary>
        /// The glyph.
        /// </summary>
        public Bitmap Glyph { get; }

        /// <summary>
        /// Creates a new instance of <see cref="AdjustedGlyph"/> with no adjustements to it.
        /// </summary>
        /// <param name="glyph">The glyph.</param>
        public AdjustedGlyph(Bitmap glyph)
        {
            WhiteSpaceAdjustment = null;
            Glyph = glyph;
        }

        /// <summary>
        /// Creates a new instance of <see cref="AdjustedGlyph"/> with no adjustements to it.
        /// </summary>
        /// <param name="glyph">The glyph.</param>
        /// <param name="adjustment">The adjustments made to reduce white space in a glyph.</param>
        public AdjustedGlyph(Bitmap glyph, WhiteSpaceAdjustment adjustment)
        {
            WhiteSpaceAdjustment = adjustment;
            Glyph = glyph;
        }
    }
}
