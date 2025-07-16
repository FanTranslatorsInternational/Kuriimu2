using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;

namespace plugin_capcom.Archives
{
    class AAPackState : ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private readonly AAPack _aatri = new();
        private List<IArchiveFile> _files;

        public IReadOnlyList<IArchiveFile> Files => _files;

        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream incStream;
            Stream datStream;

            if (filePath.GetExtensionWithDot() == ".inc")
            {
                if (!fileSystem.FileExists(filePath.GetDirectory() / "pack.dat"))
                    throw new FileNotFoundException("pack.dat not found.");

                incStream = await fileSystem.OpenFileAsync(filePath);
                datStream = await fileSystem.OpenFileAsync(filePath.GetDirectory() / "pack.dat");
            }
            else
            {
                if (!fileSystem.FileExists(filePath.GetDirectory() / "pack.inc"))
                    throw new FileNotFoundException("pack.inc not found.");

                incStream = await fileSystem.OpenFileAsync(filePath.GetDirectory() / "pack.inc");
                datStream = await fileSystem.OpenFileAsync(filePath);
            }

            var version = await AAPackSupport.GetVersion(loadContext.DialogManager);

            _files = _aatri.Load(incStream, datStream, version);
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream incStream;
            Stream datStream;

            switch (savePath.GetExtensionWithDot())
            {
                case ".inc":
                    incStream = await fileSystem.OpenFileAsync(savePath.GetDirectory() / "pack.inc", FileMode.Create, FileAccess.Write);
                    datStream = await fileSystem.OpenFileAsync(savePath.GetDirectory() / savePath.GetNameWithoutExtension() + ".dat", FileMode.Create, FileAccess.Write);
                    break;

                default:
                    incStream = await fileSystem.OpenFileAsync(savePath.GetDirectory() / savePath.GetNameWithoutExtension() + ".inc", FileMode.Create, FileAccess.Write);
                    datStream = await fileSystem.OpenFileAsync(savePath.GetDirectory() / "pack.dat", FileMode.Create, FileAccess.Write);
                    break;
            }

            _aatri.Save(incStream, datStream, _files);
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
