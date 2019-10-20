using System;

using System.Drawing;
using Kontract.Attributes;

namespace Kontract.Interfaces.Font
{
    /// <inheritdoc />
    /// <summary>
    /// The base v2 character class.
    /// </summary>
    public class FontCharacter2 : ICloneable
    {
        [FormFieldIgnore]
        public virtual uint Character { get; }

        public virtual CharacterInfo CharacterInfo { get; }

        [FormFieldIgnore]
        public virtual Bitmap Glyph { get; }

        public FontCharacter2(uint character, Bitmap glyph, CharacterInfo characterInfo)
        {
            Character = character;
            Glyph = glyph;
            CharacterInfo = characterInfo;
        }

        public virtual object Clone() => new FontCharacter2(Character, (Bitmap)Glyph.Clone(), (CharacterInfo)CharacterInfo.Clone());

        public override string ToString() => ((char)Character).ToString();
    }

    /// <summary>
    /// A transport class for character information.
    /// </summary>
    public class CharacterInfo : ICloneable
    {
        [FormField(typeof(int), nameof(CharWidth), 1, 3)]
        public int CharWidth { get; set; }

        public CharacterInfo(int charWidth)
        {
            CharWidth = charWidth;
        }

        public object Clone()
        {
            return new CharacterInfo(CharWidth);
        }
    }
}
