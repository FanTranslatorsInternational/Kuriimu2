using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;

namespace plugin_bandai_namco.Archives
{
    class SegState : ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private readonly Seg _arc = new();
        private List<IArchiveFile> _files;

        public IReadOnlyList<IArchiveFile> Files => _files;
        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var segStream = await fileSystem.OpenFileAsync(filePath);
            var binStream = await fileSystem.OpenFileAsync(filePath.ChangeExtension(".BIN"));

            var sizeName = filePath.GetDirectory() / filePath.GetNameWithoutExtension() + "SIZE.BIN";
            var sizeStream = fileSystem.FileExists(sizeName) ? await fileSystem.OpenFileAsync(sizeName) : null;

            _files = _arc.Load(segStream, binStream, sizeStream);
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var segStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);
            var binStream = await fileSystem.OpenFileAsync(savePath.ChangeExtension(".BIN"), FileMode.Create, FileAccess.Write);

            var sizeName = savePath.GetDirectory() / savePath.GetNameWithoutExtension() + "SIZE.BIN";
            var sizeStream = Files.Any(x => x.UsesCompression) ? await fileSystem.OpenFileAsync(sizeName, FileMode.Create, FileAccess.Write) : null;

            _arc.Save(segStream, binStream, sizeStream, _files);
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
