using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;
using plugin_level5.Common.Archive;
using plugin_level5.Common.Archive.Models;

namespace plugin_level5.Common.Plugins
{
    class ArchiveState : ILoadFiles, ISaveFiles, IReplaceFiles, IAddFiles, IRemoveFiles, IRenameFiles
    {
        private readonly ArchiveParser _parser = new();
        private readonly ArchiveComposer _composer = new();

        private ArchiveData? _archiveData;
        private List<IArchiveFile>? _files;
        private bool _hasDeletedFiles;
        private bool _hasAddedFiles;
        private bool _hasRenamedFiles;

        public IReadOnlyList<IArchiveFile> Files => _files ?? [];

        public bool ContentChanged => _hasDeletedFiles || _hasAddedFiles || _hasRenamedFiles || Files.Any(x => x.ContentChanged);

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

            _hasDeletedFiles = false;
            _hasAddedFiles = false;
            _hasRenamedFiles = false;
        }

        public void ReplaceFile(IArchiveFile file, Stream fileData)
        {
            ArchiveNamedEntry? entry = _archiveData?.Files.FirstOrDefault(x => x.Name == file.FilePath.ToRelative());
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
                Name = filePath.ToRelative().FullName,
                Content = fileData
            });
            _files?.Add(archiveFile);

            _hasAddedFiles = true;

            return archiveFile;
        }

        public void RenameFile(IArchiveFile file, UPath path)
        {
            ArchiveNamedEntry? entry = _archiveData?.Files.FirstOrDefault(x => x.Name == file.FilePath.ToRelative());
            if (entry is null)
                return;

            file.FilePath = path;
            entry.Name = path.ToRelative().FullName;

            _hasRenamedFiles = true;
        }

        public void RemoveFile(IArchiveFile file)
        {
            ArchiveNamedEntry? entry = _archiveData?.Files.FirstOrDefault(x => x.Name == file.FilePath.ToRelative());
            if (entry is null)
                return;

            _archiveData?.Files.Remove(entry);
            _files?.Remove(file);

            _hasRenamedFiles = true;
        }

        public void RemoveAll()
        {
            _archiveData?.Files.Clear();
            _files?.Clear();

            _hasRenamedFiles = true;
        }
    }
}
