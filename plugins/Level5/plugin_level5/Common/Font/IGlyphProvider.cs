using plugin_level5.Common.Font.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace plugin_level5.Common.Font
{
    public interface IGlyphProvider
    {
        Image<Rgba32>? GetGlyph(FontImageData fontImageData, FontGlyphData glyphData);
    }
}
