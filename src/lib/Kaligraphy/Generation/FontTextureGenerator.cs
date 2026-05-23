using Kaligraphy.Contract.DataClasses;
using Kaligraphy.Contract.DataClasses.Generation;
using Kaligraphy.Contract.DataClasses.Generation.Packing;
using Kaligraphy.Contract.Generation.Packing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Kaligraphy.Generation;

/// <summary>
/// Generates textures out of a given list of glyphs.
/// </summary>
public class FontTextureGenerator(IBinPacker<GlyphData, PackedGlyphData> packer)
{
    private static readonly GraphicsOptions Options = new();

    /// <summary>
    /// Generate font textures for the given glyphs.
    /// </summary>
    /// <param name="glyphs">The list of glyphs to pack.</param>
    /// <param name="textureCount">The maximum texture count. -1 for unlimited textures.</param>
    /// <returns>The generated textures and their packed glyphs.</returns>
    public IList<PackedGlyphsData> Generate(IList<GlyphData> glyphs, int textureCount = -1)
    {
        var fontTextures = new List<PackedGlyphsData>(Math.Max(textureCount, 0));

        IList<GlyphData> remainingGlyphs = glyphs;
        while (remainingGlyphs.Count > 0)
        {
            // Stop if the texture limit is reached
            if (textureCount >= 0 && fontTextures.Count >= textureCount)
                break;

            // Create new font texture to draw on.
            var fontCanvas = new Image<Rgba32>(packer.CanvasSize.Width, packer.CanvasSize.Height);

            // Draw each positioned glyph on the font texture
            var packedGlyphs = new List<PackedGlyphData>(remainingGlyphs.Count);
            foreach (PackedGlyphData packedGlyph in packer.Pack(remainingGlyphs))
            {
                // Ignore drawing empty, packed glyphs
                if (packedGlyph.Element.Description.Size != Size.Empty)
                    DrawGlyph(fontCanvas, packedGlyph);

                packedGlyphs.Add(packedGlyph);
            }

            var fontImage = new PackedGlyphsData
            {
                Image = fontCanvas,
                Glyphs = packedGlyphs
            };
            fontTextures.Add(fontImage);

            // Remove every handled glyph
            remainingGlyphs = [.. remainingGlyphs.Except(packedGlyphs.Select(g => g.Element))];
        }

        return fontTextures;
    }

    /// <summary>
    /// Draws a glyph onto the font texture.
    /// </summary>
    /// <param name="fontImage">The font texture to draw on.</param>
    /// <param name="packedGlyph">The adjusted glyph positioned in relation to the texture.</param>
    private static void DrawGlyph(Image<Rgba32> fontImage, PackedGlyphData packedGlyph)
    {
        GlyphData glyph = packedGlyph.Element;

        var sourceRect = new Rectangle(glyph.Description.Position, glyph.Description.Size);

        fontImage.Mutate(i => i.DrawImage(glyph.Glyph, packedGlyph.Position, sourceRect, Options));
    }
}