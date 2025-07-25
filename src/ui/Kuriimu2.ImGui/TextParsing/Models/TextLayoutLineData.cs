using System.Collections.Generic;
using SixLabors.ImageSharp;

namespace Kuriimu2.ImGui.TextParsing.Models
{
    public class TextLayoutLineData
    {
        public IList<TextLayoutCharacterData> Characters { get; set; }
        public Rectangle BoundingBox { get; set; }
    }
}
