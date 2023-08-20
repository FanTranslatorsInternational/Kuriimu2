using System.Drawing;

namespace Komponent.Font
{
    /// <summary>
    /// A class holding adjustment information for a glyph.
    /// </summary>
    public class WhiteSpaceAdjustment
    {
        /// <summary>
        /// The position into the glyph, where the non-white space starts.
        /// </summary>
        public Point GlyphPosition { get; }

        /// <summary>
        /// The size of the non-white space glyph.
        /// </summary>
        public Size GlyphSize { get; }

        /// <summary>
        /// Creates a new instance of <see cref="WhiteSpaceAdjustment"/>.
        /// </summary>
        /// <param name="glyphPosition">The position into the glyph, where the non-white space starts.</param>
        /// <param name="glyphSize">The size of the non-white space glyph.</param>
        public WhiteSpaceAdjustment(Point glyphPosition, Size glyphSize)
        {
            GlyphPosition = glyphPosition;
            GlyphSize = glyphSize;
        }
    }
}
