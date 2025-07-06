using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;

namespace plugin_ruby_party.Archives
{
    class PaaState : ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private readonly Paa _arc = new();
        private List<IArchiveFile> _files;

        public IReadOnlyList<IArchiveFile> Files => _files;
        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            var arcName = filePath.ChangeExtension(".arc");
            var arcStream = await fileSystem.OpenFileAsync(arcName);

            _files = _arc.Load(fileStream, arcStream);
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);

            var arcName = savePath.ChangeExtension(".arc");
            var arcStream = await fileSystem.OpenFileAsync(arcName, FileMode.Create, FileAccess.Write);

            _arc.Save(fileStream, arcStream, _files);
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
