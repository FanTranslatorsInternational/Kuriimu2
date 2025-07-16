using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;

namespace plugin_inti_creates.Archives
{
    class IrarcState : ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private readonly Irarc _irarc=new();
        private List<IArchiveFile> _files;

        public IReadOnlyList<IArchiveFile> Files => _files;

        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream lstStream;
            Stream arcStream;

            if (filePath.GetExtensionWithDot() == ".irlst")
            {
                var arcName = $"{filePath.GetNameWithoutExtension()}.irarc";

                if (!fileSystem.FileExists(filePath.GetDirectory() / arcName))
                    throw new FileNotFoundException($"{ arcName } not found.");

                lstStream = await fileSystem.OpenFileAsync(filePath);
                arcStream = await fileSystem.OpenFileAsync(filePath.GetDirectory() / arcName);
            }
            else
            {
                var lstName = $"{filePath.GetNameWithoutExtension()}.irlst";

                if (!fileSystem.FileExists(filePath.GetDirectory() / lstName))
                    throw new FileNotFoundException($"{lstName} not found.");

                lstStream = await fileSystem.OpenFileAsync(filePath.GetDirectory() / lstName);
                arcStream = await fileSystem.OpenFileAsync(filePath);
            }

            _files = _irarc.Load(lstStream, arcStream);
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream lstStream;
            Stream arcStream;

            var lstName = $"{savePath.GetNameWithoutExtension()}.irlst";
            var arcName = $"{savePath.GetNameWithoutExtension()}.irarc";

            switch (savePath.GetExtensionWithDot())
            {
                case ".irlst":
                    lstStream = await fileSystem.OpenFileAsync(savePath.GetDirectory() / lstName, FileMode.Create, FileAccess.Write);
                    arcStream = await fileSystem.OpenFileAsync(savePath.GetDirectory() / arcName, FileMode.Create, FileAccess.Write);
                    break;

                default:
                    lstStream = await fileSystem.OpenFileAsync(savePath.GetDirectory() / lstName, FileMode.Create, FileAccess.Write);
                    arcStream = await fileSystem.OpenFileAsync(savePath.GetDirectory() / arcName, FileMode.Create, FileAccess.Write);
                    break;
            }

            _irarc.Save(lstStream, arcStream, _files);
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
