using System;
using System.Collections.Generic;
using System.Drawing;

namespace Kontract.Interfaces
{
    /// <summary>
    /// This is the v2 font adapter interface for creating font format plugins.
    /// </summary>
    public interface IFontAdapter2
    {
        /// <summary>
        /// The list of characters provided by the font adapter to the UI.
        /// </summary>
        IEnumerable<FontCharacter2> Characters { get; set; }

        /// <summary>
        /// Character baseline.
        /// </summary>
        float Baseline { get; set; }

        /// <summary>
        /// Character descent line.
        /// </summary>
        float DescentLine { get; set; }
    }

    ///// <summary>
    ///// This interface allows the font adapter to add new characters through the UI.
    ///// </summary>
    //public interface IAddCharacters2
    //{
    //    /// <summary>
    //    /// Creates a new character and allows the plugin to provide its derived type.
    //    /// </summary>
    //    /// <returns>FontCharacter or a derived type.</returns>
    //    FontCharacter2 NewCharacter(FontCharacter2 selectedCharacter = null);

    //    /// <summary>
    //    /// Adds a newly created character to the file and allows the plugin to perform any required adding steps.
    //    /// </summary>
    //    /// <param name="character"></param>
    //    /// <returns>True if the character was added, False otherwise.</returns>
    //    bool AddCharacter(FontCharacter2 character);
    //}

    ///// <summary>
    ///// This interface allows the font adapter to delete characters through the UI.
    ///// </summary>
    //public interface IDeleteCharacters2
    //{
    //    /// <summary>
    //    /// Deletes an character and allows the plugin to perform any required deletion steps.
    //    /// </summary>
    //    /// <param name="character">The character to be deleted.</param>
    //    /// <returns>True if the character was successfully deleted, False otherwise.</returns>
    //    bool DeleteCharacter(FontCharacter2 character);
    //}

    ///// <summary>
    ///// Characters provide an extended properties dialog?
    ///// </summary>
    //public interface ICharactersHaveExtendedProperties2
    //{
    //    // TODO: Figure out how to best implement this feature with WPF.
    //    /// <summary>
    //    /// Opens the extended properties dialog for an character.
    //    /// </summary>
    //    /// <param name="character">The character to view and/or edit extended properties for.</param>
    //    /// <returns>True if changes were made, False otherwise.</returns>
    //    bool ShowCharacterProperties(FontCharacter2 character);
    //}

    /// <inheritdoc />
    /// <summary>
    /// The base v2 character class.
    /// </summary>
    public class FontCharacter2 : ICloneable
    {
        public virtual uint Character { get; } = 'A';

        public virtual CharWidthInfo WidthInfo { get; } = null;

        public virtual Bitmap Glyph { get; set; } = null;

        public virtual int GlyphWidth { get; } = 0;

        public virtual int GlyphHeight { get; } = 0;

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
