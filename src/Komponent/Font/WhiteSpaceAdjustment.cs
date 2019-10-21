using System.Drawing;

namespace Komponent.Font
{
    public class WhiteSpaceAdjustment
    {
        public Bitmap Glyph { get; }

        public Point GlyphPosition { get; }

        public Size GlyphSize { get; }

        public WhiteSpaceAdjustment(Bitmap glyph, Point glyphPosition, Size glyphSize)
        {
            Glyph = glyph;
            GlyphPosition = glyphPosition;
            GlyphSize = glyphSize;
        }
    }
}
