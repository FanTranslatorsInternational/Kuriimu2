using Kaligraphy.Contract.DataClasses;
using Kaligraphy.Contract.DataClasses.Generation;
using Kaligraphy.Contract.DataClasses.Generation.Packing;
using Kaligraphy.Generation.Packing;
using SixLabors.ImageSharp;

using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Kaligraphy.Generation;

/// <summary>
/// Generates textures out of a given list of glyphs.
/// </summary>
public class FontTextureGenerator
{
    private static readonly GraphicsOptions _options = new();

    private readonly Size _canvasSize;
    private readonly FontBinPacker _fontPacker;

    /// <summary>
    /// Creates a new instance of <see cref="FontTextureGenerator"/>.
    /// </summary>
    /// <param name="canvasSize">The size of the canvas to draw on.</param>
    /// <param name="margin">The margin to the top and left side of each texture.</param>
    public FontTextureGenerator(Size canvasSize, int margin)
    {
        _canvasSize = canvasSize;
        _fontPacker = new FontBinPacker(canvasSize, margin);
    }

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
            var fontCanvas = new Image<Rgba32>(_canvasSize.Width, _canvasSize.Height);

            // Draw each positioned glyph on the font texture
            var packedGlyphs = new List<PackedGlyphData>(remainingGlyphs.Count);
            foreach (PackedGlyphData packedGlyph in _fontPacker.Pack(remainingGlyphs))
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
            remainingGlyphs = remainingGlyphs.Except(packedGlyphs.Select(g => g.Element)).ToList();
        }

        return fontTextures;
    }

    /// <summary>
    /// Draws a glyph onto the font texture.
    /// </summary>
    /// <param name="fontImage">The font texture to draw on.</param>
    /// <param name="packedGlyph">The adjusted glyph positioned in relation to the texture.</param>
    private void DrawGlyph(Image<Rgba32> fontImage, PackedGlyphData packedGlyph)
    {
        GlyphData glyph = packedGlyph.Element;

        var sourceRect = new Rectangle(glyph.Description.Position, glyph.Description.Size);

        fontImage.Mutate(i => i.DrawImage(glyph.Glyph, packedGlyph.Position, sourceRect, _options));
    }
}