using System.Collections.Generic;
using System.Linq;
using Kaligraphy.Contract.Rendering;
using Konnect.Contract.DataClasses.Plugin.File.Font;

namespace Kuriimu2.ImGui.TextParsing
{
    class FontPluginGlyphProvider : IGlyphProvider
    {
        private readonly Dictionary<ushort, Kaligraphy.Contract.DataClasses.Rendering.CharacterInfo> _characterLookup = [];
        private readonly IReadOnlyList<CharacterInfo> _characters;

        public FontPluginGlyphProvider(IReadOnlyList<CharacterInfo> characters)
        {
            _characters = characters;
        }

        public Kaligraphy.Contract.DataClasses.Rendering.CharacterInfo? GetOrDefault(ushort codePoint)
        {
            if (_characterLookup.TryGetValue(codePoint, out Kaligraphy.Contract.DataClasses.Rendering.CharacterInfo? cachedInfo))
                return cachedInfo;

            CharacterInfo? foundInfo = _characters.FirstOrDefault(x => x.CodePoint == codePoint);
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

        public int GetMaxHeight() => _characters.Max(c => c.GlyphPosition.Y + c.BoundingBox.Height);
    }
}
