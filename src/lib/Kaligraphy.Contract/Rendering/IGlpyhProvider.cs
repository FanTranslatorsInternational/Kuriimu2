using Kaligraphy.Contract.DataClasses.Rendering;

namespace Kaligraphy.Contract.Rendering
{
    public interface IGlyphProvider
    {
        CharacterInfo? GetOrDefault(ushort codePoint);

        int GetMaxHeight(); // _characters.Max(c => c.GlyphPosition.Y + c.BoundingBox.Height)
    }
}
