using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.DataClasses.Plugin.File.Font;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Font;
using plugin_level5.Common.Font;
using plugin_level5.Common.Font.Models;
using SixLabors.ImageSharp;

namespace plugin_level5.Common.Plugins
{
    internal class FntState : ILoadFiles, ISaveFiles, IAddCharacters, IRemoveCharacters
    {
        private readonly IPluginFileManager _fileManager;

        private bool _isChanged;

        private FontImageData? _fontImageData;
        private List<FontSet> _sets;
        private List<List<CharacterInfo>> _characters;

        public IReadOnlyList<FontSet> Sets => _sets;

        public bool ContentChanged => IsContentChanged();

        public FntState(IPluginFileManager fileManager)
        {
            _fileManager = fileManager;
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream filestream = await fileSystem.OpenFileAsync(filePath);

            var fontParser = new FontParser(loadContext.DialogManager, _fileManager);
            _fontImageData = await fontParser.Parse(filestream);

            if (_fontImageData is null)
                throw new InvalidOperationException("No font data loaded.");

            _isChanged = false;
            _sets = [];
            _characters = [];

            // Create character infos
            IGlyphProvider glyphProvider = _fontImageData.Platform is PlatformType.Ctr
                ? new GlyphCtrProvider()
                : new GlyphDefaultProvider();

            FontGlyphsData largeFont = _fontImageData.Font.LargeFont;

            var largeCharacters = new List<CharacterInfo>();
            var largeSet = new FontSet { Characters = largeCharacters, Name = "Default" };

            foreach (char codePoint in largeFont.Glyphs.Keys)
            {
                largeCharacters.Add(new CharacterInfo
                {
                    CodePoint = codePoint,
                    BoundingBox = new Size(largeFont.Glyphs[codePoint].Width, largeFont.MaxHeight),
                    GlyphPosition = new Point(largeFont.Glyphs[codePoint].Description.X, largeFont.Glyphs[codePoint].Description.Y),
                    Glyph = glyphProvider.GetGlyph(_fontImageData, largeFont.Glyphs[codePoint]),
                    ContentChanged = false
                });
            }

            FontGlyphsData smallFont = _fontImageData.Font.SmallFont;

            var smallCharacters = new List<CharacterInfo>();
            var smallSet = new FontSet { Characters = smallCharacters, Name = "Furigana" };

            foreach (char codePoint in smallFont.Glyphs.Keys)
            {
                smallCharacters.Add(new CharacterInfo
                {
                    CodePoint = codePoint,
                    BoundingBox = new Size(smallFont.Glyphs[codePoint].Width, smallFont.MaxHeight),
                    GlyphPosition = new Point(smallFont.Glyphs[codePoint].Description.X, smallFont.Glyphs[codePoint].Description.Y),
                    Glyph = glyphProvider.GetGlyph(_fontImageData, smallFont.Glyphs[codePoint]),
                    ContentChanged = false
                });
            }

            _sets = [largeSet, smallSet];
            _characters = [largeCharacters, smallCharacters];
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

            var fontComposer = new FontComposer(_fileManager);

            _fontImageData = fontGenerator.Generate(_fontImageData, _characters[0], _characters[1]);
            await fontComposer.Compose(_fontImageData, fileStream);

            _isChanged = false;
        }

        public CharacterInfo CreateCharacterInfo(char codePoint)
        {
            return new CharacterInfo
            {
                CodePoint = codePoint,
                BoundingBox = new Size(_fontImageData!.Font.LargeFont.Glyphs.Values.Max(x => x.Description.Height), _fontImageData!.Font.LargeFont.MaxHeight),
                GlyphPosition = Point.Empty
            };
        }

        public bool AddCharacter(FontSet set, CharacterInfo characterInfo)
        {
            var setIndex = _sets.IndexOf(set);

            if (_characters[setIndex].Contains(characterInfo))
                return false;

            _characters[setIndex].Add(characterInfo);
            _isChanged = true;

            return true;
        }

        public bool RemoveCharacter(FontSet set, CharacterInfo characterInfo)
        {
            var setIndex = _sets.IndexOf(set);

            if (!_characters[setIndex].Contains(characterInfo))
                return false;

            _characters[setIndex].Remove(characterInfo);
            _isChanged = true;

            return true;
        }

        public void RemoveAll(FontSet set)
        {
            var setIndex = _sets.IndexOf(set);

            _characters[setIndex].Clear();
            _isChanged = true;
        }

        private bool IsContentChanged()
        {
            return _characters.Any(x => x.Any(y => y.ContentChanged)) || _isChanged;
        }
    }
}
