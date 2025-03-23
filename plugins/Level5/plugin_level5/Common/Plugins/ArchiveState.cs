using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;
using plugin_level5.Common.Archive;
using plugin_level5.Common.Archive.Models;

namespace plugin_level5.Common.Plugins
{
    class ArchiveState : ILoadFiles, ISaveFiles, IReplaceFiles, IAddFiles, IRemoveFiles
    {
        private readonly ArchiveParser _parser = new();
        private readonly ArchiveComposer _composer = new();

        private ArchiveData? _archiveData;
        private List<IArchiveFile>? _files;

        public IReadOnlyList<IArchiveFile> Files => _files ?? [];

        public bool ContentChanged => _files?.Any(x => x.ContentChanged) ?? false;

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            _archiveData = _parser.Parse(fileStream);

            var files = new List<IArchiveFile>();
            foreach (ArchiveNamedEntry file in _archiveData.Files)
            {
                files.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = file.Name,
                    FileData = file.Content
                }));
            }

            _files = files;
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            if (_archiveData is null)
                return;

            Stream output = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);

            _composer.Compose(_archiveData, output);
        }

        public void ReplaceFile(IArchiveFile file, Stream fileData)
        {
            ArchiveNamedEntry? entry = _archiveData?.Files.FirstOrDefault(x => x.Name == file.FilePath);
            if (entry is null)
                return;

            entry.Content = fileData;
            file.SetFileData(fileData);
        }

        public IArchiveFile AddFile(Stream fileData, UPath filePath)
        {
            var archiveFile = new ArchiveFile(new ArchiveFileInfo
            {
                FilePath = filePath,
                FileData = fileData
            });

            _archiveData?.Files.Add(new ArchiveNamedEntry
            {
                Name = filePath.FullName,
                Content = fileData
            });
            _files?.Add(archiveFile);

            return archiveFile;
        }

        public void RemoveFile(IArchiveFile file)
        {
            ArchiveNamedEntry? entry = _archiveData?.Files.FirstOrDefault(x => x.Name == file.FilePath);
            if (entry is null)
                return;

            _archiveData?.Files.Remove(entry);
            _files?.Remove(file);
        }

        public void RemoveAll()
        {
            _archiveData?.Files.Clear();
            _files?.Clear();
        }
    }
}
