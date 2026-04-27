using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Archive;

namespace plugin_level5.N3DS.Archive
{
    internal class Arc0State : ILoadFiles, ISaveFiles, IReplaceFiles, IAddFiles, IRemoveFiles, IRenameFiles
    {
        private readonly Arc0 _arc0 = new();

        private List<Arc0ArchiveFile>? _files;
        private bool _hasDeletedFiles;
        private bool _hasAddedFiles;
        private bool _hasRenamedFiles;

        public IReadOnlyList<IArchiveFile> Files => _files ?? [];
        public bool ContentChanged => _hasDeletedFiles || _hasAddedFiles || _hasRenamedFiles || Files.Any(x => x.ContentChanged);

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            _files = _arc0.Load(fileStream);
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);
            _arc0.Save(fileStream, _files!);

            _hasDeletedFiles = false;
            _hasAddedFiles = false;
            _hasRenamedFiles = false;
        }

        public void ReplaceFile(IArchiveFile file, Stream fileData)
        {
            file.SetFileData(fileData);
        }

        public void RenameFile(IArchiveFile file, UPath path)
        {
            file.FilePath = path;
            _hasRenamedFiles = true;
        }

        public void RemoveFile(IArchiveFile file)
        {
            _files?.Remove((Arc0ArchiveFile)file);
            _hasDeletedFiles = true;
        }

        public void RemoveAll()
        {
            _files?.Clear();
            _hasDeletedFiles = true;
        }

        public IArchiveFile AddFile(Stream fileData, UPath filePath)
        {
            var fileInfo = new ArchiveFileInfo
            {
                FileData = fileData,
                FilePath = filePath.FullName
            };
            var newAfi = new Arc0ArchiveFile(fileInfo, new Arc0FileEntry());
            _files?.Add(newAfi);

            _hasAddedFiles = true;

            return newAfi;
        }
    }
}
