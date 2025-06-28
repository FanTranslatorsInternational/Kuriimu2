using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Archive;

namespace plugin_ganbarion.Archives
{
    class JarcState : ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private string _magic;
        private Jarc _jarc = new();
        private Jcmp _jcmp = new();
        private List<IArchiveFile> _files;

        public IReadOnlyList<IArchiveFile> Files => _files;
        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream, true);
            _magic = br.PeekString(4);

            switch (_magic)
            {
                case "jARC":
                    _files = _jarc.Load(fileStream);
                    break;

                case "jCMP":
                    _files = _jcmp.Load(fileStream);
                    break;
            }
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);

            switch (_magic)
            {
                case "jARC":
                    _jarc.Save(fileStream, _files);
                    break;

                case "jCMP":
                    _jcmp.Save(fileStream, _files);
                    break;
            }
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
