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

        public Image<Rgba32>? GetGlyph(FontImageData fontImageData, FontGlyphData glyphData)
        {
            if (glyphData.Description.Width <= 0 || glyphData.Description.Height <= 0)
                return null;

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
