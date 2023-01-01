using System;
using System.Drawing;

namespace Kontract.Models.Plugins.State.Font
{
    /// <summary>
    /// A class representing one character of a font.
    /// </summary>
    public class CharacterInfo : ICloneable
    {
        /// <summary>
        /// The code point of this character.
        /// </summary>
        public uint CodePoint { get; }

        /// <summary>
        /// The size of the character.
        /// </summary>
        public Size CharacterSize { get; protected set; }

        /// <summary>
        /// The glyph of the character.
        /// </summary>
        public System.Drawing.Image Glyph { get; protected set; }

        /// <summary>
        /// Determines if the content of this character was changed.
        /// </summary>
        /// <remarks>Should only be set by this class or the responsible plugin.</remarks>
        public bool ContentChanged { get; set; }

        public CharacterInfo(uint codePoint, Size characterSize, System.Drawing.Image glyph)
        {
            CodePoint = codePoint;
            CharacterSize = characterSize;
            Glyph = glyph;
        }

        /// <summary>
        /// Sets the character size.
        /// </summary>
        /// <param name="newCharacterSize">The new character size for this instance.</param>
        public void SetCharacterSize(Size newCharacterSize)
        {
            if (CharacterSize == newCharacterSize)
                return;

            CharacterSize = newCharacterSize;
            ContentChanged = true;
        }

        /// <summary>
        /// Sets the glyph.
        /// </summary>
        /// <param name="newGlyph">The new glyph for this instance.</param>
        public void SetGlyph(System.Drawing.Image newGlyph)
        {
            if (Glyph == newGlyph)
                return;

            Glyph = newGlyph;
            ContentChanged = true;
        }

        /// <inheritdoc cref="Clone"/>
        public virtual object Clone()
        {
            return new CharacterInfo(CodePoint, CharacterSize, (System.Drawing.Image)Glyph.Clone());
        }

        /// <inheritdoc cref="ToString"/>
        public override string ToString()
        {
            return ((char)CodePoint).ToString();
        }
    }
}
