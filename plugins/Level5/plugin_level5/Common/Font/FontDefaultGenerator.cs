using Kaligraphy.Contract.DataClasses.Generation.Packing;
using Kaligraphy.Contract.DataClasses.Generation;
using Kaligraphy.Contract.DataClasses;
using Kaligraphy.Generation;
using Konnect.Contract.DataClasses.Plugin.File.Font;
using plugin_level5.Common.Font.Models;
using SixLabors.ImageSharp;

namespace plugin_level5.Common.Font
{
    class FontDefaultGenerator : IFontGenerator
    {
        public FontImageData Generate(FontImageData fontImageData, IList<CharacterInfo> characters)
        {
            // Pack glyphs
            Size canvasSize = fontImageData.Images[0].Image.ImageInfo.ImageSize;
            var textureGenerator = new FontTextureGenerator(canvasSize, 1);

            GlyphData[] glyphData = characters
                .Where(c => c.Glyph is not null)
                .Select(c => new GlyphData
                {
                    Character = c.CodePoint,
                    Glyph = c.Glyph!,
                    Description = new GlyphDescriptionData
                    {
                        Position = Point.Empty,
                        Size = c.Glyph!.Size
                    }
                })
                .ToArray();
            IList<PackedGlyphsData> glyphImages = textureGenerator.Generate(glyphData, fontImageData.Images.Length);

            // Set image
            var characterLookup = characters.ToDictionary(x => x.CodePoint);

            var largeGlyphs = new Dictionary<char, FontGlyphData>();

            var imageIndex = 0;
            foreach (PackedGlyphsData glyphImage in glyphImages)
            {
                foreach (PackedGlyphData glyph in glyphImage.Glyphs)
                    largeGlyphs[glyph.Element.Character] = new FontGlyphData
                    {
                        CodePoint = glyph.Element.Character,
                        Width = characterLookup[glyph.Element.Character].BoundingBox.Width,
                        Location = new FontGlyphLocationData
                        {
                            Index = imageIndex,
                            X = glyph.Position.X,
                            Y = glyph.Position.Y
                        },
                        Description = new FontGlyphDescriptionData
                        {
                            X = (sbyte)characterLookup[glyph.Element.Character].GlyphPosition.X,
                            Y = (sbyte)characterLookup[glyph.Element.Character].GlyphPosition.Y,
                            Width = (byte)glyph.Element.Glyph.Width,
                            Height = (byte)glyph.Element.Glyph.Height
                        }
                    };

                fontImageData.Images[imageIndex++].Image.SetImage(glyphImage.Image);
            }

            // Set glyph data
            fontImageData.Font.SmallFont = new FontGlyphsData
            {
                Glyphs = new Dictionary<char, FontGlyphData>()
            };

            //  Set glyphs without representation on channel 0
            foreach (CharacterInfo character in characters.Where(c => c.Glyph is null))
                largeGlyphs[character.CodePoint] = new FontGlyphData
                {
                    CodePoint = character.CodePoint,
                    Width = character.BoundingBox.Width,
                    Location = new FontGlyphLocationData
                    {
                        Index = imageIndex,
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

            fontImageData.Font.LargeFont = new FontGlyphsData
            {
                Glyphs = largeGlyphs,
                MaxHeight = characters.Max(c => c.BoundingBox.Height),
                FallbackCharacter = '?'
            };

            return fontImageData;
        }
    }
}
