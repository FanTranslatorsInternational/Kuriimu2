using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;

namespace plugin_koei_tecmo.Archives
{
    class IdxState : ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private readonly Idx _arc = new();
        private List<IArchiveFile> _files;

        public IReadOnlyList<IArchiveFile> Files => _files;
        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream idxStream;
            Stream binStream;

            if (filePath.GetExtensionWithDot() == ".idx")
            {
                idxStream = await fileSystem.OpenFileAsync(filePath);
                binStream = await fileSystem.OpenFileAsync(filePath.ChangeExtension(".bin"));
            }
            else
            {
                idxStream = await fileSystem.OpenFileAsync(filePath.ChangeExtension(".idx"));
                binStream = await fileSystem.OpenFileAsync(filePath);
            }

            _files = _arc.Load(idxStream, binStream);
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream idxStream;
            Stream binStream;

            if (savePath.GetExtensionWithDot() == ".idx")
            {
                idxStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);
                binStream = await fileSystem.OpenFileAsync(savePath.ChangeExtension(".bin"), FileMode.Create, FileAccess.Write);
            }
            else
            {
                idxStream = await fileSystem.OpenFileAsync(savePath.ChangeExtension(".idx"), FileMode.Create, FileAccess.Write);
                binStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);
            }

            _arc.Save(idxStream, binStream, _files);
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
