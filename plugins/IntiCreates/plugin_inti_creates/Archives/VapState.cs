using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Archive;

namespace plugin_inti_creates.Archives
{
    class VapState : ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private readonly Vap _vap = new();
        private List<IArchiveFile> _files;

        public IReadOnlyList<IArchiveFile> Files => _files;

        public bool ContentChanged => IsContentChange();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            _files = _vap.Load(fileStream);
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);
            _vap.Save(fileStream, _files);
        }

        public void ReplaceFile(IArchiveFile afi, Stream fileData)
        {
            afi.SetFileData(fileData);
        }

        private bool IsContentChange()
        {
            return _files.Any(x => x.ContentChanged);
        }
    }
}
