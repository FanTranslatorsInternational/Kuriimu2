using SixLabors.ImageSharp;

namespace Kaligraphy.Contract.DataClasses.Layout
{
    public class TextLayoutLineData
    {
        public IList<TextLayoutCharacterData> Characters { get; set; }
        public Rectangle BoundingBox { get; set; }
    }
}
