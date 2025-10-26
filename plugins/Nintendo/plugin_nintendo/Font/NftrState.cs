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

        public IReadOnlyList<CharacterInfo> Characters => _fontData.Characters;
        public float Baseline { get; set; }
        public float DescentLine { get; set; }

        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            _fontData = _reader.Read(fileStream);
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

        public bool AddCharacter(CharacterInfo characterInfo)
        {
            _fontData.Characters.Add(characterInfo);
            return true;
        }

        public bool RemoveCharacter(CharacterInfo characterInfo)
        {
            return _fontData.Characters.Remove(characterInfo);
        }

        public void RemoveAll()
        {
            _fontData.Characters.Clear();
        }
    }
}
