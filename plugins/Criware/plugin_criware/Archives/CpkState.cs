using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Archive;

namespace plugin_criware.Archives
{
    class CpkState : ILoadFiles, ISaveFiles, IReplaceFiles, IRemoveFiles
    {
        private readonly Cpk _cpk = new();
        private List<IArchiveFile> _files;
        private bool _filesDeleted;

        public IReadOnlyList<IArchiveFile> Files => _files;
        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            _filesDeleted = false;

            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            _files = _cpk.Load(fileStream);
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);
            _cpk.Save(fileStream, _files);
        }

        public void ReplaceFile(IArchiveFile afi, Stream fileData)
        {
            afi.SetFileData(fileData);
        }

        private bool IsContentChanged()
        {
            return Files.Any(x => x.ContentChanged) || _filesDeleted;
        }

        public void RemoveFile(IArchiveFile afi)
        {
            _files.Remove(afi);
            _cpk.DeleteFile(afi);

            _filesDeleted = true;
        }

        public void RemoveAll()
        {
            _files.Clear();
            _cpk.DeleteAll();

            _filesDeleted = true;
        }
    }
}
