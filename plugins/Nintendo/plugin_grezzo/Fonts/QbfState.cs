using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.DataClasses.Plugin.File.Font;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Font;
using SixLabors.ImageSharp;

namespace plugin_grezzo.Fonts
{
    class QbfState : ILoadFiles, ISaveFiles, IAddCharacters, IRemoveCharacters
    {
        private readonly Qbf _qbf = new();
        private List<CharacterInfo> _characters;
        private FontSet _set;

        public IReadOnlyList<FontSet> Sets => [_set];
        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            _characters = _qbf.Load(fileStream);
            _set = new FontSet { Characters = _characters };
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);
            _qbf.Save(fileStream, _characters);
        }

        private bool IsContentChanged()
        {
            return _characters.Any(x => x.ContentChanged);
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

            _characters.Add(characterInfo);
            return true;
        }

        public bool RemoveCharacter(FontSet set, CharacterInfo characterInfo)
        {
            if (_set != set)
                return false;

            return _characters.Remove(characterInfo);
        }

        public void RemoveAll(FontSet set)
        {
            if (_set != set)
                return;

            _characters.Clear();
        }
    }
}
