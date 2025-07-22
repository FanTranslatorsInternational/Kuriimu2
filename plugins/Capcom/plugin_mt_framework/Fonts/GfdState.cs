using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.DataClasses.Plugin.File.Font;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Font;
using Konnect.Extensions;
using SixLabors.ImageSharp;

namespace plugin_mt_framework.Fonts
{
    class GfdState : ILoadFiles, ISaveFiles, IAddCharacters, IRemoveCharacters
    {
        private readonly Gfdv1 _fontv1 = new();
        private readonly Gfdv2 _fontv2 = new();

        private FontVersion _version;
        private List<CharacterInfo> _characters;

        public IReadOnlyList<CharacterInfo> Characters => _characters;
        public float Baseline { get; set; }
        public float DescentLine { get; set; }
        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            _version = GfdSupport.PeekVersion(fileStream);
            _characters = _version switch
            {
                FontVersion.V1 => await _fontv1.Load(fileStream, fileSystem, filePath.GetDirectory(), loadContext.DialogManager!),
                FontVersion.V2 => await _fontv2.Load(fileStream, fileSystem, filePath.GetDirectory(), loadContext.DialogManager!),
                _ => throw new InvalidOperationException($"Invalid font version {_version}.")
            };
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);

            switch (_version)
            {
                case FontVersion.V1:
                    _fontv1.Save(_characters, fileStream, fileSystem, savePath.GetDirectory());
                    break;

                case FontVersion.V2:
                    _fontv2.Save(_characters, fileStream, fileSystem, savePath.GetDirectory());
                    break;

                default:
                    throw new InvalidOperationException($"Invalid font version {_version}.");
            }
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
