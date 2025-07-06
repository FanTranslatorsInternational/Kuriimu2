using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Archive;

namespace plugin_mt_framework.Archives
{
    class MtArcState : ILoadFiles, ISaveFiles, IReplaceFiles, IAddFiles, IRenameFiles, IRemoveFiles
    {
        private readonly MtArc _arc = new();
        private List<IArchiveFile> _files;

        private bool _hasAddedFiles;
        private bool _hasDeletedFiles;

        public IReadOnlyList<IArchiveFile> Files => _files;

        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            _hasAddedFiles = false;
            _hasDeletedFiles = false;

            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            var platform = MtArcSupport.DeterminePlatform(fileStream);
            _files = _arc.Load(fileStream, platform);
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);
            _arc.Save(fileStream, _files);
        }

        public void ReplaceFile(IArchiveFile afi, Stream fileData)
        {
            afi.SetFileData(fileData);
        }

        public IArchiveFile AddFile(Stream fileData, UPath filePath)
        {
            IArchiveFile afi = _arc.Add(fileData, filePath);
            _files.Add(afi);

            _hasAddedFiles = true;

            return afi;
        }

        public void RenameFile(IArchiveFile afi, UPath path)
        {
            afi.FilePath = path;
        }

        public void RemoveFile(IArchiveFile afi)
        {
            _files.Remove(afi);
            _hasDeletedFiles = true;
        }

        public void RemoveAll()
        {
            _files.Clear();
            _hasDeletedFiles = true;
        }

        private bool IsContentChanged()
        {
            return _hasAddedFiles || _hasDeletedFiles || Files.Any(x => x.ContentChanged);
        }
    }
}
