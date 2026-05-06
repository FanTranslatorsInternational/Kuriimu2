using Kaligraphy.Contract.Rendering;
using Konnect.Contract.DataClasses.Plugin.File.Font;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Konnect.Plugin.File.Font;

public class FontPluginGlyphProvider(IReadOnlyList<CharacterInfo> characters) : IGlyphProvider
{
    private readonly Dictionary<ushort, Kaligraphy.Contract.DataClasses.Rendering.CharacterInfo> _characterLookup = [];

    public Kaligraphy.Contract.DataClasses.Rendering.CharacterInfo? GetOrDefault(ushort codePoint)
    {
        if (_characterLookup.TryGetValue(codePoint, out Kaligraphy.Contract.DataClasses.Rendering.CharacterInfo? cachedInfo))
            return cachedInfo;

        CharacterInfo? foundInfo = characters.FirstOrDefault(x => x.CodePoint == codePoint);
        if (foundInfo is null)
            return null;

        return _characterLookup[codePoint] = new Kaligraphy.Contract.DataClasses.Rendering.CharacterInfo
        {
            CodePoint = foundInfo.CodePoint,
            GlyphPosition = foundInfo.GlyphPosition,
            BoundingBox = foundInfo.BoundingBox,
            Glyph = foundInfo.Glyph
        };
    }

    public Kaligraphy.Contract.DataClasses.Rendering.CharacterInfo? GetOrDefault(ushort codePoint, Color textColor)
    {
        Kaligraphy.Contract.DataClasses.Rendering.CharacterInfo? characterInfo = GetOrDefault(codePoint);
        if (characterInfo?.Glyph is null)
            return null;

        characterInfo.Glyph = characterInfo.Glyph.Clone(c => c.Filter(CreateColorMatrix(textColor)));

        return characterInfo;
    }

    public int GetMaxHeight() => characters.Count <= 0 ? 0 : characters.Max(c => c.GlyphPosition.Y + c.BoundingBox.Height);

    private static ColorMatrix CreateColorMatrix(Color targetColor)
    {
        var pixel = targetColor.ToPixel<Rgba32>();
        float targetR = pixel.R / 255f;
        float targetG = pixel.G / 255f;
        float targetB = pixel.B / 255f;

        return new ColorMatrix(
            0f, 0f, 0f, 0f,
            0f, 0f, 0f, 0f,
            0f, 0f, 0f, 0f,
            0f, 0f, 0f, 1f,
            targetR, targetG, targetB, 0f);
    }
}