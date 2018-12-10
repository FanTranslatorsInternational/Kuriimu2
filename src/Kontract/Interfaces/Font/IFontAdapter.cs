using System;
using System.Collections.Generic;
using System.Drawing;
using Kontract.Attributes;

namespace Kontract.Interfaces.Font
{
    /// <summary>
    /// This is the font adapter interface for creating font format plugins.
    /// </summary>
    public interface IFontAdapter
    {
        /// <summary>
        /// The list of characters provided by the font adapter to the UI.
        /// </summary>
        IEnumerable<FontCharacter> Characters { get; set; }

        /// <summary>
        /// The list of textures provided by the font adapter to the UI.
        /// </summary>
        List<Bitmap> Textures { get; set; }

        /// <summary>
        /// Character baseline.
        /// </summary>
        float Baseline { get; set; }

        /// <summary>
        /// Character descent line.
        /// </summary>
        float DescentLine { get; set; }
    }

    /// <summary>
    /// This interface allows the font adapter to add new characters through the UI.
    /// </summary>
    public interface IAddCharacters
    {
        /// <summary>
        /// Creates a new character and allows the plugin to provide its derived type.
        /// </summary>
        /// <returns>FontCharacter or a derived type.</returns>
        FontCharacter NewCharacter(FontCharacter selectedCharacter = null);

        /// <summary>
        /// Adds a newly created character to the file and allows the plugin to perform any required adding steps.
        /// </summary>
        /// <param name="character"></param>
        /// <returns>True if the character was added, False otherwise.</returns>
        bool AddCharacter(FontCharacter character);
    }

    /// <summary>
    /// This interface allows the font adapter to delete characters through the UI.
    /// </summary>
    public interface IDeleteCharacters
    {
        /// <summary>
        /// Deletes an character and allows the plugin to perform any required deletion steps.
        /// </summary>
        /// <param name="character">The character to be deleted.</param>
        /// <returns>True if the character was successfully deleted, False otherwise.</returns>
        bool DeleteCharacter(FontCharacter character);
    }

    /// <summary>
    /// Characters provide an extended properties dialog?
    /// </summary>
    public interface ICharactersHaveExtendedProperties
    {
        // TODO: Figure out how to best implement this feature with WPF.
        /// <summary>
        /// Opens the extended properties dialog for an character.
        /// </summary>
        /// <param name="character">The character to view and/or edit extended properties for.</param>
        /// <returns>True if changes were made, False otherwise.</returns>
        bool ShowCharacterProperties(FontCharacter character);
    }

    /// <summary>
    /// The base character class.
    /// </summary>
    public class FontCharacter : ICloneable
    {
        [FormField(typeof(char), "Character", 1, 1)]
        public virtual uint Character { get; set; } = 'A';

        [FormField(typeof(int), "Texture ID")]
        public virtual int TextureID { get; set; } = 0;

        [FormField(typeof(int), "X")]
        public virtual int GlyphX { get; set; } = 0;

        [FormField(typeof(int), "Y")]
        public virtual int GlyphY { get; set; } = 0;

        [FormField(typeof(int), "Width")]
        public virtual int GlyphWidth { get; set; } = 0;

        [FormField(typeof(int), "Height")]
        public virtual int GlyphHeight { get; set; } = 0;

        public virtual object Clone() => new FontCharacter
        {
            Character = Character,
            TextureID = TextureID,
            GlyphX = GlyphX,
            GlyphY = GlyphY,
            GlyphWidth = GlyphWidth,
            GlyphHeight = GlyphHeight
        };

        public override string ToString() => ((char)Character).ToString();
    }
}
