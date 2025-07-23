using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.DataClasses.Plugin.File.Font;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Font;

namespace plugin_grezzo.Fonts
{
    class QbfState : ILoadFiles, ISaveFiles, IFontFilePluginState
    {
        private readonly Qbf _qbf = new();
        private List<CharacterInfo> _characters;

        public IReadOnlyList<CharacterInfo> Characters => _characters;
        public float Baseline { get; set; }
        public float DescentLine { get; set; }
        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            _characters = _qbf.Load(fileStream);
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
    }
}
