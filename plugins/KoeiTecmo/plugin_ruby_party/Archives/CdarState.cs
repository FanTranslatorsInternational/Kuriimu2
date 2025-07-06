using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Archive;

namespace plugin_ruby_party.Archives
{
    class CdarState : ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private readonly Cdar _cdar = new();
        private List<IArchiveFile> _files;

        public IReadOnlyList<IArchiveFile> Files => _files;

        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            _files = _cdar.Load(fileStream);
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);
            _cdar.Save(fileStream, _files);
        }

        public void ReplaceFile(IArchiveFile afi, Stream fileData)
        {
            afi.SetFileData(fileData);
        }

        private bool IsContentChanged()
        {
            return _files.Any(x => x.ContentChanged);
        }
    }
}
