using Kaligraphy.Contract.DataClasses.Rendering;
using SixLabors.ImageSharp;

namespace Kaligraphy.Contract.Rendering;

public interface IGlyphProvider
{
    CharacterInfo? GetOrDefault(ushort codePoint);
    CharacterInfo? GetOrDefault(ushort codePoint, Color textColor);

    int GetMaxHeight();
}