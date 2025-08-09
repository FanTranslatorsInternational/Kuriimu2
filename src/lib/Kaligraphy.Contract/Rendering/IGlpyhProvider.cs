using Kaligraphy.Contract.DataClasses.Rendering;

namespace Kaligraphy.Contract.Rendering
{
    public interface IGlyphProvider
    {
        CharacterInfo? GetOrDefault(ushort codePoint);

        int GetMaxHeight();
    }
}
