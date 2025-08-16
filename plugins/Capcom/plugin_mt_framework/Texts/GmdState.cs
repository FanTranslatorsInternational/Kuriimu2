using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.DataClasses.Plugin.File.Text;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Text;

namespace plugin_mt_framework.Texts
{
    class GmdState : ILoadFiles, ISaveFiles, ITextFilePluginState
    {
        private readonly Gmdv1 _gmd1 = new();
        private readonly Gmdv2 _gmd2 = new();

        private GmdVersion _version;
        private List<TextEntry> _texts;

        public IReadOnlyList<TextEntry> Texts => _texts;
        public IReadOnlyList<Guid>? Previews { get; } = [Guid.Parse("1280108e-010d-4bf0-a495-e614f340360c")];
        public ITextEntryPager? Pager { get; } = null;

        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            if (!GmdSupport.TryGetVersion(fileStream, out GmdVersion version))
                throw new InvalidOperationException("GMD version unknown.");

            _version = version;
            _texts = version switch
            {
                GmdVersion.v1 => _gmd1.Load(fileStream),
                GmdVersion.v2 => _gmd2.Load(fileStream),
                _ => throw new InvalidOperationException("GMD version unknown.")
            };
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            throw new NotImplementedException();
        }

        private bool IsContentChanged()
        {
            return _texts.Any(x => x.ContentChanged);
        }
    }
}
