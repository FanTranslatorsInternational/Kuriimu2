using plugin_level5.Common.Font.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace plugin_level5.Common.Font
{
    class GlyphCtrProvider : IGlyphProvider
    {
        private const float ChannelScaling_ = 255f / (255f - 123f); // Scales color channel between 0-255 after subtracting 0x7B
        private const float ChannelTranslation_ = -(123f * ChannelScaling_) / 255f; // Subtraction by 0x7B, correctly scaled for scaling between 0-255

        private readonly GraphicsOptions _options = new();

        private readonly ColorMatrix[] _colorMatrices0 =
        [
            new(0f, 0f, 0f, ChannelScaling_,
                0f, 0f, 0f, 0f,
                0f, 0f, 0f, 0f,
                0f, 0f, 0f, 0f,
                1f, 1f, 1f, ChannelTranslation_),
            new(0f, 0f, 0f, 0f,
                0f, 0f, 0f, ChannelScaling_,
                0f, 0f, 0f, 0f,
                0f, 0f, 0f, 0f,
                1f, 1f, 1f, ChannelTranslation_),
            new(0f, 0f, 0f, 0f,
                0f, 0f, 0f, 0f,
                0f, 0f, 0f, ChannelScaling_,
                0f, 0f, 0f, 0f,
                1f, 1f, 1f, ChannelTranslation_)
        ];

        private readonly ColorMatrix[] _colorMatrices1 =
        [
            new(0f, 0f, 0f, 1f,
                0f, 0f, 0f, 0f,
                0f, 0f, 0f, 0f,
                0f, 0f, 0f, 0f,
                1f, 1f, 1f, 0f),
            new(0f, 0f, 0f, 0f,
                0f, 0f, 0f, 1f,
                0f, 0f, 0f, 0f,
                0f, 0f, 0f, 0f,
                1f, 1f, 1f, 0f),
            new(0f, 0f, 0f, 0f,
                0f, 0f, 0f, 0f,
                0f, 0f, 0f, 1f,
                0f, 0f, 0f, 0f,
                1f, 1f, 1f, 0f)
        ];

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

            Image<Rgba32> image = fontImageData.Images[0].Image.GetImage();

            switch (fontImageData.Font.Version.Version)
            {
                case 0:
                    return image.Clone(context => context.Crop(srcRect).Filter(_colorMatrices0[glyphData.Location.Index]));

                case 1:
                    return image.Clone(context => context.Crop(srcRect).Filter(_colorMatrices1[glyphData.Location.Index]));

                default:
                    throw new InvalidOperationException($"Unknown font version {fontImageData.Font.Version.Version} for platform {fontImageData.Platform}.");
            }
        }
    }
}
