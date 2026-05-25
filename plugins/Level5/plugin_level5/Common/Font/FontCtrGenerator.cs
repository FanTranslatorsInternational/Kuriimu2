using Kaligraphy.Contract.DataClasses;
using Kaligraphy.Contract.DataClasses.Generation;
using Kaligraphy.Contract.DataClasses.Generation.Packing;
using Kaligraphy.Generation;
using Kaligraphy.Generation.Packing;
using Konnect.Contract.DataClasses.Plugin.File.Font;
using plugin_level5.Common.Font.Models;
using RectangleBinPacking;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace plugin_level5.Common.Font
{
    class FontCtrGenerator : IFontGenerator
    {
        private const float ChannelScalingReverse_ = (255f - 123f) / 255f;
        private const float ChannelTranslationReverse_ = 123f / 255f;

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

        public FontImageData Generate(FontImageData fontImageData, IList<CharacterInfo> largeCharacters, IList<CharacterInfo> smallCharacters)
        {
            // Pack glyphs
            Size canvasSize = fontImageData.Images[0].Image.ImageInfo.ImageSize;
            var packer = new GuillotineGlyphFontBinPacker(canvasSize, 1, false, GuillotineBinPack.FreeRectChoiceHeuristic.RectBestAreaFit);
            var textureGenerator = new FontTextureGenerator(packer);

            IList<GlyphData> glyphs = CreateGlyphData(largeCharacters, smallCharacters);
            IList<PackedGlyphsData> glyphImages = textureGenerator.Generate(glyphs, 3);

            // Set image
            var finalImage = new Image<Rgba32>(canvasSize.Width, canvasSize.Height);

            var largeCharacterLookup = largeCharacters.ToDictionary(x => x.CodePoint);
            var smallCharacterLookup = smallCharacters.ToDictionary(x => x.CodePoint);

            var largeGlyphs = new Dictionary<char, FontGlyphData>();
            var smallGlyphs = new Dictionary<char, FontGlyphData>();

            var imageIndex = 0;
            foreach (PackedGlyphsData glyphImage in glyphImages)
            {
                foreach (PackedGlyphData glyph in glyphImage.Glyphs)
                {
                    var isFurigana = ((Level5GlyphData)glyph.Element).IsFurigana;
                    var fontCharacters = isFurigana ? smallCharacterLookup : largeCharacterLookup;

                    var character = fontCharacters[glyph.Element.Character];
                    var fontGlyph = CreateFontGlyph(character, glyph, imageIndex);

                    var fontGlyphs = isFurigana ? smallGlyphs : largeGlyphs;
                    fontGlyphs[glyph.Element.Character] = fontGlyph;
                }

                switch (fontImageData.Font.Version.Version)
                {
                    case 0:
                        glyphImage.Image.Mutate(context => context.Filter(_inverseColorMatrices0[imageIndex++]));
                        finalImage.Mutate(context => context.DrawImage(glyphImage.Image, PixelColorBlendingMode.Add, 1f));
                        break;

                    case 1:
                        glyphImage.Image.Mutate(context => context.Filter(_inverseColorMatrices1[imageIndex++]));
                        finalImage.Mutate(context => context.DrawImage(glyphImage.Image, PixelColorBlendingMode.Add, 1f));
                        break;

                    default:
                        throw new InvalidOperationException($"Unknown font version {fontImageData.Font.Version.Version} for platform {fontImageData.Platform}.");
                }
            }

            fontImageData.Images[0].Image.SetImage(finalImage);

            // Set empty glyphs on channel 0
            foreach (CharacterInfo character in largeCharacters.Where(c => c.Glyph is null))
                largeGlyphs[character.CodePoint] = CreateEmptyFontGlyph(character);

            foreach (CharacterInfo character in smallCharacters.Where(c => c.Glyph is null))
                smallGlyphs[character.CodePoint] = CreateEmptyFontGlyph(character);

            fontImageData.Font.LargeFont = new FontGlyphsData
            {
                Glyphs = largeGlyphs,
                MaxHeight = largeCharacters.Max(c => c.BoundingBox.Height),
                FallbackCharacter = '?'
            };

            fontImageData.Font.SmallFont = new FontGlyphsData
            {
                Glyphs = smallGlyphs,
                MaxHeight = smallCharacters.Max(c => c.BoundingBox.Height),
                FallbackCharacter = '?'
            };

            return fontImageData;
        }

        private IList<GlyphData> CreateGlyphData(IList<CharacterInfo> largeCharacters, IList<CharacterInfo> smallCharacters)
        {
            return largeCharacters
                .Where(c => c.Glyph is not null)
                .Select(c => CreateGlyphData(c, false))
                .Concat(smallCharacters
                    .Where(c => c.Glyph is not null)
                    .Select(c => CreateGlyphData(c, true)))
                .ToArray<GlyphData>();
        }

        private Level5GlyphData CreateGlyphData(CharacterInfo character, bool isFurigana)
        {
            return new Level5GlyphData
            {
                IsFurigana = isFurigana,
                Character = character.CodePoint,
                Glyph = character.Glyph!,
                Description = new BorderSpaceData
                {
                    Position = Point.Empty,
                    Size = character.Glyph!.Size
                }
            };
        }

        private FontGlyphData CreateFontGlyph(CharacterInfo character, PackedGlyphData glyph, int imageIndex)
        {
            return new FontGlyphData
            {
                CodePoint = glyph.Element.Character,
                Width = character.BoundingBox.Width,
                Location = new FontGlyphLocationData
                {
                    Index = imageIndex,
                    X = glyph.Position.X,
                    Y = glyph.Position.Y
                },
                Description = new FontGlyphDescriptionData
                {
                    X = (sbyte)character.GlyphPosition.X,
                    Y = (sbyte)character.GlyphPosition.Y,
                    Width = (byte)glyph.Element.Glyph.Width,
                    Height = (byte)glyph.Element.Glyph.Height
                }
            };
        }

        private FontGlyphData CreateEmptyFontGlyph(CharacterInfo character)
        {
            return new FontGlyphData
            {
                CodePoint = character.CodePoint,
                Width = character.BoundingBox.Width,
                Location = new FontGlyphLocationData
                {
                    Index = 0,
                    X = 0,
                    Y = 0
                },
                Description = new FontGlyphDescriptionData
                {
                    X = 0,
                    Y = 0,
                    Width = 0,
                    Height = 0
                }
            };
        }
    }
}
