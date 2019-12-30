using System;
using Kontract.Attributes;

namespace Kontract.Interfaces.Plugins.State.Font
{
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
