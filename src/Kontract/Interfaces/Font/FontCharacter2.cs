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
        public virtual uint Character { get; }

        public virtual CharacterInfo CharacterInfo { get; }

        public virtual Bitmap Glyph { get; }

        public FontCharacter2(uint character, Bitmap glyph, CharacterInfo characterInfo)
        {
            Character = character;
            Glyph = glyph;
            CharacterInfo = characterInfo;
        }

        public virtual object Clone() => new FontCharacter2(Character, (Bitmap)Glyph.Clone(), CharacterInfo);

        public override string ToString() => ((char)Character).ToString();
    }

    /// <summary>
    /// A transport class for character information.
    /// </summary>
    public struct CharacterInfo
    {
        public int CharWidth { get; }
        public CharacterPosition CharacterPosition { get; }

        public CharacterInfo(int charWidth, CharacterPosition position)
        {
            CharWidth = charWidth;
            CharacterPosition = position;
        }
    }

    /// <summary>
    /// A transport class for character positioning.
    /// </summary>
    public struct CharacterPosition
    {
        public int Top { get; }
        public int Left { get; }

        public CharacterPosition(int top, int left)
        {
            Top = top;
            Left = left;
        }
    }
}
