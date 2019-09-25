using System;

using System.Drawing;

namespace Kontract.Interfaces.Font
{
    /// <inheritdoc />
    /// <summary>
    /// The base v2 character class.
    /// </summary>
    public class FontCharacter2 : ICloneable
    {
        public virtual uint Character { get; set; } = 'A';

        public virtual CharWidthInfo WidthInfo { get; set; } = null;

        public virtual Bitmap Glyph { get; set; } = null;

        public virtual int GlyphWidth { get; set; } = 0;

        public virtual int GlyphHeight { get; set; } = 0;

        public virtual object Clone() => new FontCharacter
        {
            Character = Character,
            GlyphWidth = GlyphWidth,
            GlyphHeight = GlyphHeight
        };

        public override string ToString() => ((char)Character).ToString();
    }

    /// <summary>
    /// A transport class for character width info.
    /// </summary>
    public class CharWidthInfo
    {
        public int Left { get; set; }
        public int GlyphWidth { get; set; }
        public int CharWidth { get; set; }
    }
}
