using plugin_level5.Common.Font.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace plugin_level5.Common.Font
{
    class GlyphDefaultProvider : IGlyphProvider
    {
        private readonly GraphicsOptions _options = new();

        public Image<Rgba32> GetGlyph(FontImageData fontImageData, FontGlyphData glyphData)
        {
            int glyphWidth, glyphHeight;

            if (glyphData.Description.Width <= 0 || glyphData.Description.Height <= 0)
            {
                glyphWidth = glyphData.Description.Width <= 0 ? glyphData.Width : glyphData.Description.Width;
                glyphHeight = glyphData.Description.Height <= 0 ? fontImageData.Font.LargeFont.MaxHeight : glyphData.Description.Height;

                return new Image<Rgba32>(glyphWidth, glyphHeight, Color.Transparent);
            }

            Image<Rgba32> rawGlyph = GetRawGlyph(fontImageData, glyphData);

            glyphWidth = Math.Max(glyphData.Description.Width, glyphData.Width);
            glyphHeight = Math.Max(glyphData.Description.Height, fontImageData.Font.LargeFont.MaxHeight);

            var glyph = new Image<Rgba32>(glyphWidth, glyphHeight);

            glyph.Mutate(context => context.DrawImage(rawGlyph, new Point(glyphData.Description.X, glyphData.Description.Y), _options));

            return glyph;
        }

        private Image<Rgba32> GetRawGlyph(FontImageData fontImageData, FontGlyphData glyphData)
        {
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
