using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.DataClasses.Plugin.File.Font;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Font;
using plugin_nintendo.Font.DataClasses;
using SixLabors.ImageSharp;

namespace plugin_nintendo.Font
{
    class NftrState : ILoadFiles, ISaveFiles, IAddCharacters, IRemoveCharacters
    {
        private readonly NftrReader _reader = new();
        private readonly NftrWriter _writer = new();

        private NftrData _fontData;
        private FontSet _set;

        public IReadOnlyList<FontSet> Sets => [_set];

        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            _fontData = _reader.Read(fileStream);
            _set = new FontSet { Characters = _fontData.Characters };
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);
            _writer.Write(fileStream, _fontData);
        }

        private bool IsContentChanged()
        {
            return _fontData.Characters.Any(x => x.ContentChanged);
        }

        public CharacterInfo CreateCharacterInfo(char codePoint)
        {
            return new CharacterInfo
            {
                CodePoint = codePoint,
                BoundingBox = new Size(15, 15),
                GlyphPosition = Point.Empty
            };
        }

        public bool AddCharacter(FontSet set, CharacterInfo characterInfo)
        {
            if (_set != set)
                return false;

            _fontData.Characters.Add(characterInfo);
            return true;
        }

        public bool RemoveCharacter(FontSet set, CharacterInfo characterInfo)
        {
            if (_set != set)
                return false;

            return _fontData.Characters.Remove(characterInfo);
        }

        public void RemoveAll(FontSet set)
        {
            if (_set != set)
                return;

            _fontData.Characters.Clear();
        }
    }
}
