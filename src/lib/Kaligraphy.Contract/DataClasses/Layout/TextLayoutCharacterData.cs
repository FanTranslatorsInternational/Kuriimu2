using Kaligraphy.Contract.DataClasses.Parsing;
using SixLabors.ImageSharp;

namespace Kaligraphy.Contract.DataClasses.Layout
{
    public class TextLayoutCharacterData
    {
        public CharacterData Character { get; set; }
        public Rectangle BoundingBox { get; set; }
        public Rectangle GlyphBoundingBox { get; set; }
    }
}
