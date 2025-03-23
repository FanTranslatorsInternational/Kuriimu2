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
        private readonly WhiteSpaceMeasurer _whitespaceMeasurer = new();

        public FontImageData Generate(FontImageData fontImageData, IList<CharacterInfo> characters)
        {
            // Pack glyphs
            Size canvasSize = fontImageData.Images[0].Image.ImageInfo.ImageSize;
            var textureGenerator = new FontTextureGenerator(canvasSize, 1);

            IList<PackedGlyphsData> glyphImages = textureGenerator.Generate(characters.Select(c => new GlyphData
            {
                Character = c.CodePoint,
                Glyph = c.Glyph!,
                Description = _whitespaceMeasurer.MeasureWhiteSpace(c.Glyph!)
            }).ToArray(), fontImageData.Images.Length);

            // Set image
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

                fontImageData.Images[imageIndex++].Image.SetImage(glyphImage.Image);
            }

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
