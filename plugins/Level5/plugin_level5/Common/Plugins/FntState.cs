using Kaligraphy.Contract.DataClasses;
using Kaligraphy.Generation;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.DataClasses.Plugin.File.Font;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Font;
using plugin_level5.Common.Font;
using plugin_level5.Common.Font.Models;
using plugin_level5.Common.Image.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace plugin_level5.Common.Plugins
{
    internal class FntState : ILoadFiles, ISaveFiles, IAddCharacters, IRemoveCharacters
    {
        private readonly FontComposer _fontComposer = new();

        private bool _isChanged;

        private FontImageData? _fontImageData;
        private List<CharacterInfo> _characters;

        public IReadOnlyList<CharacterInfo> Characters => _characters;

        public float Baseline { get; set; }

        public float DescentLine { get => 0; set { } }

        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream filestream = await fileSystem.OpenFileAsync(filePath);

            var fontParser = new FontParser(loadContext.DialogManager!);
            _fontImageData = await fontParser.Parse(filestream);

            if (_fontImageData is null)
                throw new InvalidOperationException("No font data loaded.");

            _isChanged = false;
            _characters = [];

            // Create character infos
            IGlyphProvider glyphProvider = _fontImageData.Platform is PlatformType.Ctr
                ? new GlyphCtrProvider()
                : new GlyphDefaultProvider();

            FontGlyphsData largeFont = _fontImageData.Font.LargeFont;
            foreach (char codePoint in largeFont.Glyphs.Keys)
            {
                Image<Rgba32> glyph = glyphProvider.GetGlyph(_fontImageData, largeFont.Glyphs[codePoint]);

                _characters.Add(new CharacterInfo
                {
                    CodePoint = codePoint,
                    CharacterSize = glyph.Size,
                    Glyph = glyph,
                    ContentChanged = false
                });
            }
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            if (_fontImageData is null)
                return;

            Stream fileStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);

            // Set character infos
            IFontGenerator fontGenerator = _fontImageData.Platform is PlatformType.Ctr
                ? new FontCtrGenerator()
                : new FontDefaultGenerator();

            _fontImageData = fontGenerator.Generate(_fontImageData, _characters);
            _fontComposer.Compose(_fontImageData, fileStream);

            _isChanged = false;
        }

        public CharacterInfo CreateCharacterInfo(char codePoint)
        {
            return new CharacterInfo
            {
                CodePoint = codePoint
            };
        }

        public bool AddCharacter(CharacterInfo characterInfo)
        {
            if (_characters.Contains(characterInfo))
                return false;

            _characters.Add(characterInfo);
            _isChanged = true;

            return true;
        }

        public bool RemoveCharacter(CharacterInfo characterInfo)
        {
            if (!_characters.Contains(characterInfo))
                return false;

            _characters.Remove(characterInfo);
            _isChanged = true;

            return true;
        }

        public void RemoveAll()
        {
            _characters.Clear();
            _isChanged = true;
        }

        private bool IsContentChanged()
        {
            return _characters.Any(x => x.ContentChanged) || _isChanged;
        }
    }
}
