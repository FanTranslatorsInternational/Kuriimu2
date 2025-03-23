namespace plugin_level5.Common.Font.Models
{
    public class FontGlyphsData
    {
        public ushort FallbackCharacter { get; set; }
        public int MaxHeight { get; set; }
        public IDictionary<char, FontGlyphData> Glyphs { get; set; }
    }
}
