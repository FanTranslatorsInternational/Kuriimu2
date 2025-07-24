using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File.Font;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File.Font;
using Konnect.Contract.Plugin.File;
using SixLabors.ImageSharp;

namespace plugin_grezzo.Fonts
{
    class GzfState : ILoadFiles, ISaveFiles, IFontFilePluginState, IAddCharacters, IRemoveCharacters
    {
        private readonly Gzf _gzf = new();
        private List<CharacterInfo> _characters;

        public IReadOnlyList<CharacterInfo> Characters => _characters;
        public float Baseline { get; set; }
        public float DescentLine { get; set; }
        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            _characters = _gzf.Load(fileStream);
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);
            _gzf.Save(fileStream, _characters);
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

        public bool AddCharacter(CharacterInfo characterInfo)
        {
            _characters.Add(characterInfo);
            return true;
        }

        public bool RemoveCharacter(CharacterInfo characterInfo)
        {
            return _characters.Remove(characterInfo);
        }

        public void RemoveAll()
        {
            _characters.Clear();
        }
    }
}