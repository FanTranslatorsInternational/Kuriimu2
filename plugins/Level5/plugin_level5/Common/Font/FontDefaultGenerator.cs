using Kaligraphy.Contract.DataClasses;
using Kaligraphy.Contract.DataClasses.Generation;
using Kaligraphy.Contract.DataClasses.Generation.Packing;
using Kaligraphy.Generation;
using Kaligraphy.Generation.Packing;
using Konnect.Contract.DataClasses.Plugin.File.Font;
using plugin_level5.Common.Font.Models;
using plugin_level5.Common.Image.Models;
using RectangleBinPacking;
using SixLabors.ImageSharp;

namespace plugin_level5.Common.Font
{
    class FontDefaultGenerator : IFontGenerator
    {
        public FontImageData Generate(FontImageData fontImageData, IList<CharacterInfo> largeCharacters, IList<CharacterInfo> smallCharacters)
        {
            // Pack glyphs
            Size canvasSize = fontImageData.Images[0].Image.ImageInfo.ImageSize;
            var packer = new GuillotineGlyphFontBinPacker(canvasSize, 1, false, GuillotineBinPack.FreeRectChoiceHeuristic.RectBestAreaFit);
            var textureGenerator = new FontTextureGenerator(packer);

            IList<GlyphData> glyphs = CreateGlyphData(largeCharacters, smallCharacters);
            IList<PackedGlyphsData> glyphImages = textureGenerator.Generate(glyphs);

            // Resize image list
            var images = new ImageData[glyphImages.Count];
            for (var i = 0; i < glyphImages.Count; i++)
            {
                if (i < fontImageData.Images.Length)
                {
                    images[i] = fontImageData.Images[i];
                    continue;
                }

                images[i] = new ImageData
                {
                    Version = fontImageData.Images[0].Version,
                    LegacyData = fontImageData.Images[0].LegacyData,
                    Image = fontImageData.Images[0].Image.Clone(),
                    KtxState = null
                };
            }

            fontImageData.Images = images;

            // Set images
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

                fontImageData.Images[imageIndex++].Image.SetImage(glyphImage.Image);
            }

            // Set empty glyphs on image 0
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
