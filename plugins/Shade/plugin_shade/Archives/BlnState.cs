using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;

namespace plugin_shade.Archives
{
    class BlnState : ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private readonly Bln _bln = new();
        private List<IArchiveFile> _files;

        public IReadOnlyList<IArchiveFile> Files => _files;

        public bool ContentChanged => IsChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream dataStream;
            Stream indexStream;

            switch (filePath.GetName())
            {
                case "mcb1.bln":
                    dataStream = await fileSystem.OpenFileAsync(filePath);
                    indexStream = await fileSystem.OpenFileAsync(filePath.GetDirectory() / "mcb0.bln");
                    break;

                default:
                    indexStream = await fileSystem.OpenFileAsync(filePath);
                    dataStream = await fileSystem.OpenFileAsync(filePath.GetDirectory() / "mcb1.bln");
                    break;
            }

            if (dataStream == null || indexStream == null)
                throw new InvalidOperationException("This is no Bln archive.");

            _files = _bln.Load(indexStream, dataStream);
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream dataOutput;
            Stream indexOutput;

            switch (savePath.GetName())
            {
                case "mcb1.bln":
                    dataOutput = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.ReadWrite);
                    indexOutput = await fileSystem.OpenFileAsync(savePath.GetDirectory() / "mcb0.bln", FileMode.Create, FileAccess.ReadWrite);
                    break;

                default:
                    indexOutput = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.ReadWrite);
                    dataOutput = await fileSystem.OpenFileAsync(savePath.GetDirectory() / "mcb1.bln", FileMode.Create, FileAccess.ReadWrite);
                    break;
            }

            _bln.Save(indexOutput, dataOutput, _files);
        }

        public void ReplaceFile(IArchiveFile afi, Stream fileData)
        {
            afi.SetFileData(fileData);
        }

        private bool IsChanged()
        {
            return Files.Any(x => x.ContentChanged);
        }
    }
}
