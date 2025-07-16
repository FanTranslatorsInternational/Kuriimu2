using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Archive;

namespace plugin_yuusha_shisu.Archives
{
    public class PacState : ILoadFiles, ISaveFiles, IArchiveFilePluginState
    {
        private readonly Pac _pac = new();
        private List<IArchiveFile> _files;

        public IReadOnlyList<IArchiveFile> Files => _files;

        public bool ContentChanged => IsChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            _files = _pac.Load(fileStream);
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream saveStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);
            _pac.Save(saveStream, _files);
        }

        private bool IsChanged()
        {
            return _files.Any(x => x.ContentChanged);
        }
    }
}
