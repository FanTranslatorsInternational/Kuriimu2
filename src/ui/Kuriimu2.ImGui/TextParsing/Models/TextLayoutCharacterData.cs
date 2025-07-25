using SixLabors.ImageSharp;

namespace Kuriimu2.ImGui.TextParsing.Models
{
    public class TextLayoutCharacterData
    {
        public CharacterData Character { get; set; }
        public Rectangle BoundingBox { get; set; }
        public Rectangle GlyphBoundingBox { get; set; }
    }
}
