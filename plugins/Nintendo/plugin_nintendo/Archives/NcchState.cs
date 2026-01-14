using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_nintendo.Archives
{
    class NcchState : ILoadFiles, ISaveFiles, IReplaceFiles, IAddFiles, IRenameFiles, IRemoveFiles
    {
        private readonly Ncch _ncch = new();

        private List<IArchiveFile> _files;

        public IReadOnlyList<IArchiveFile> Files => _files;

        public bool ContentChanged => IsChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            _files = _ncch.Load(fileStream);
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream output = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.ReadWrite);
            _ncch.Save(output, _files);
        }

        public void ReplaceFile(IArchiveFile afi, Stream fileData)
        {
            afi.SetFileData(fileData);
        }

        public IArchiveFile AddFile(Stream fileData, UPath filePath)
        {
            var newFile = new ArchiveFile(new ArchiveFileInfo
            {
                FilePath = filePath,
                FileData = fileData,
                ContentChanged = true
            });

            _files.Add(newFile);

            return newFile;
        }

        public void RenameFile(IArchiveFile file, UPath path)
        {
            file.FilePath = path;
        }

        private bool IsChanged()
        {
            return Files.Any(x => x.ContentChanged);
        }

        public void RemoveFile(IArchiveFile file)
        {
            _files.Remove(file);
        }

        public void RemoveAll()
        {
            _files.Clear();
        }
    }
}
