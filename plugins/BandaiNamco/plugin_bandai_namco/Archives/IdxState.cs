using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;

namespace plugin_bandai_namco.Archives
{
    class IdxState : ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private readonly Idx _arc = new();
        private List<IArchiveFile> _files;

        public IReadOnlyList<IArchiveFile> Files => _files;
        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            var apkFilePaths = fileSystem.EnumerateFiles(filePath.GetDirectory(), "*.apk");
            var apkStreams = apkFilePaths.Select(x => fileSystem.OpenFile(x)).ToArray();

            _files = _arc.Load(fileStream, apkStreams);
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);

            // Create new APK's where files are changed
            var apkStreams = new List<(UPath, Stream)>();
            foreach (var path in _arc.ApkPaths)
            {
                var isChanged = _files.Where(x => x.FilePath.IsInDirectory(path.ToAbsolute(), true)).Any(x => x.ContentChanged);
                if (isChanged)
                    apkStreams.Add((path, await fileSystem.OpenFileAsync(savePath.GetDirectory() / path.GetName(), FileMode.Create, FileAccess.Write)));
            }

            _arc.Save(fileStream, apkStreams, _files);
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
