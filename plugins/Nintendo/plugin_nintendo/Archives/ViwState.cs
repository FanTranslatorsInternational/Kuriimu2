using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;

namespace plugin_nintendo.Archives
{
    class ViwState : ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private readonly Viw _arc = new();

        private List<IArchiveFile> _files;

        public IReadOnlyList<IArchiveFile> Files => _files;
        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var viwStream = await fileSystem.OpenFileAsync(filePath);
            var infStream = await fileSystem.OpenFileAsync(filePath.ChangeExtension(".inf"));
            var dataStream = await fileSystem.OpenFileAsync(filePath.ChangeExtension(""));
            _files = _arc.Load(viwStream, infStream, dataStream);
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var viwStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);
            var infStream = await fileSystem.OpenFileAsync(savePath.ChangeExtension(".inf"), FileMode.Create, FileAccess.Write);
            var dataStream = await fileSystem.OpenFileAsync(savePath.ChangeExtension(""), FileMode.Create, FileAccess.Write);
            _arc.Save(viwStream, infStream, dataStream, _files);
        }

        public void ReplaceFile(IArchiveFile afi, Stream fileData)
        {
            afi.SetFileData(fileData);
        }

        private bool IsContentChanged()
        {
            return Files.Any(x => x.ContentChanged);
        }
    }
}
