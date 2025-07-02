using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;

namespace plugin_square_enix.Archives
{
    class SarState : ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private readonly Sar _arc = new();
        private List<IArchiveFile> _files;

        public IReadOnlyList<IArchiveFile> Files => _files;
        public bool ContentChanged => IsContentChange();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var dataStream = await fileSystem.OpenFileAsync(filePath);
            var matStream = await fileSystem.OpenFileAsync(filePath.ChangeExtension(".sar.mat"));

            _files = _arc.Load(dataStream, matStream);
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var dataStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);
            var matStream = await fileSystem.OpenFileAsync(savePath.ChangeExtension(".sar.mat"), FileMode.Create, FileAccess.Write);

            _arc.Save(dataStream, matStream, _files);
        }


        public void ReplaceFile(IArchiveFile afi, Stream fileData)
        {
            afi.SetFileData(fileData);
        }

        private bool IsContentChange()
        {
            return _files.Any(x => x.ContentChanged);
        }
    }
}
