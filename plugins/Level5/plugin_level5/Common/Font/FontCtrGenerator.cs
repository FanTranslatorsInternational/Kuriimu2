using Kaligraphy.Contract.DataClasses;
using Kaligraphy.Contract.DataClasses.Generation;
using Kaligraphy.Contract.DataClasses.Generation.Packing;
using Kaligraphy.Generation;
using Konnect.Contract.DataClasses.Plugin.File.Font;
using plugin_level5.Common.Font.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace plugin_level5.Common.Font
{
    class FontCtrGenerator : IFontGenerator
    {
        private const float ChannelScalingReverse_ = (255f - 123f) / 255f;
        private const float ChannelTranslationReverse_ = 123f / 255f;

        private readonly WhiteSpaceMeasurer _whitespaceMeasurer = new();

        private readonly ColorMatrix[] _inverseColorMatrices0 =
        [
            new(0f, 0f, 0f, 0f,
                0f, 0f, 0f, 0f,
                0f, 0f, 0f, 0f,
                ChannelScalingReverse_, 0f, 0f, 0f,
                ChannelTranslationReverse_, 0f, 0f, 1f),
            new(0f, 0f, 0f, 0f,
                0f, 0f, 0f, 0f,
                0f, 0f, 0f, 0f,
                0f, ChannelScalingReverse_, 0f, 0f,
                0f, ChannelTranslationReverse_, 0f, 1f),
            new(0f, 0f, 0f, 0f,
                0f, 0f, 0f, 0f,
                0f, 0f, 0f, 0f,
                0f, 0f, ChannelScalingReverse_, 0f,
                0f, 0f, ChannelTranslationReverse_, 1f)
        ];

        private readonly ColorMatrix[] _inverseColorMatrices1 =
        [
            new(0f, 0f, 0f, 1f,
                0f, 0f, 0f, 0f,
                0f, 0f, 0f, 0f,
                1f, 0f, 0f, 0f,
                0f, 0f, 0f, 1f),
            new(0f, 0f, 0f, 0f,
                0f, 0f, 0f, 1f,
                0f, 0f, 0f, 0f,
                0f, 1f, 0f, 0f,
                0f, 0f, 0f, 1f),
            new(0f, 0f, 0f, 0f,
                0f, 0f, 0f, 0f,
                0f, 0f, 0f, 1f,
                0f, 0f, 1f, 0f,
                0f, 0f, 0f, 1f)
        ];

        public FontImageData Generate(FontImageData fontImageData, IList<CharacterInfo> characters)
        {
            // Pack glyphs
            Size canvasSize = fontImageData.Images[0].Image.ImageInfo.ImageSize;
            var textureGenerator = new FontTextureGenerator(canvasSize, 1);

            var glyphData = characters.Select(c => new GlyphData
            {
                Character = c.CodePoint,
                Glyph = c.Glyph!,
                Description = _whitespaceMeasurer.MeasureWhiteSpace(c.Glyph!)
            }).ToArray();
            IList<PackedGlyphsData> glyphImages = textureGenerator.Generate(glyphData, 3);

            // Set image
            var finalImage = new Image<Rgba32>(canvasSize.Width, canvasSize.Height);
            var largeGlyphs = new Dictionary<char, FontGlyphData>();

            var imageIndex = 0;
            foreach (PackedGlyphsData glyphImage in glyphImages)
            {
                foreach (PackedGlyphData glyph in glyphImage.Glyphs)
                    largeGlyphs[glyph.Element.Character] = new FontGlyphData
                    {
                        CodePoint = glyph.Element.Character,
                        Width = glyph.Element.Glyph.Width,
                        Location = new FontGlyphLocationData
                        {
                            Index = imageIndex,
                            X = glyph.Position.X,
                            Y = glyph.Position.Y
                        },
                        Description = new FontGlyphDescriptionData
                        {
                            X = (sbyte)glyph.Element.Description.Position.X,
                            Y = (sbyte)glyph.Element.Description.Position.Y,
                            Width = (byte)glyph.Element.Description.Size.Width,
                            Height = (byte)glyph.Element.Description.Size.Height
                        }
                    };

                switch (fontImageData.Font.Version.Version)
                {
                    case 0:
                        finalImage.Mutate(context => context.DrawImage(glyphImage.Image, 1f).Filter(_inverseColorMatrices0[imageIndex++]));
                        break;

                    case 1:
                        finalImage.Mutate(context => context.DrawImage(glyphImage.Image, 1f).Filter(_inverseColorMatrices1[imageIndex++]));
                        break;

                    default:
                        throw new InvalidOperationException($"Unknown font version {fontImageData.Font.Version.Version} for platform {fontImageData.Platform}.");
                }
            }

            fontImageData.Images[0].Image.SetImage(finalImage);

            // Set glyph data
            fontImageData.Font.SmallFont = new FontGlyphsData
            {
                Glyphs = new Dictionary<char, FontGlyphData>()
            };

            fontImageData.Font.LargeFont = new FontGlyphsData
            {
                Glyphs = largeGlyphs,
                MaxHeight = characters.Max(c => c.CharacterSize!.Value.Height),
                FallbackCharacter = '?'
            };

            return fontImageData;
        }
    }
}
