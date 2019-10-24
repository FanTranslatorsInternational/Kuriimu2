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
        public virtual uint Character { get; set; }

        public virtual CharacterInfo CharacterInfo { get; set; }

        [FormFieldIgnore]
        public virtual Bitmap Glyph { get; set; }

        public FontCharacter2(uint character)
        {
            Character = character;
        }

        public virtual object Clone()
        {
            var character = new FontCharacter2(Character);
            character.Glyph = (Bitmap)Glyph.Clone();
            character.CharacterInfo = (CharacterInfo)CharacterInfo.Clone();
            return character;
        }

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
