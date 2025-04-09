using plugin_level5.Common.Font.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace plugin_level5.Common.Font
{
    class GlyphDefaultProvider : IGlyphProvider
    {
        public Image<Rgba32>? GetGlyph(FontImageData fontImageData, FontGlyphData glyphData)
        {
            if (glyphData.Description.Width <= 0 || glyphData.Description.Height <= 0)
                return null;

            var srcRect = new Rectangle(
                glyphData.Location.X,
                glyphData.Location.Y,
                glyphData.Description.Width,
                glyphData.Description.Height);

            Image<Rgba32> image = fontImageData.Images[glyphData.Location.Index].Image.GetImage();

            return image.Clone(context => context.Crop(srcRect));
        }
    }
}
