namespace plugin_level5.Common.Font.Models
{
    public class FontGlyphData
    {
        public ushort CodePoint { get; set; }
        public int Width { get; set; }

        public FontGlyphLocationData Location { get; set; }
        public FontGlyphDescriptionData Description { get; set; }
    }
}
