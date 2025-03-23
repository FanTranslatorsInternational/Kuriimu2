using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;

namespace plugin_atlus.N3DS.Archive
{
    class HpiHpbState : ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private HpiHpb _hpiHpb = new();

        public List<IArchiveFile> _files;

        public IReadOnlyList<IArchiveFile> Files => _files;

        public bool ContentChanged => _files.Any(x => x.ContentChanged);

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream hpiStream, hpbStream;

            if (filePath.GetExtensionWithDot() == ".HPI")
            {
                var hpbName = filePath.GetNameWithoutExtension() + ".HPB";

                if (!fileSystem.FileExists(filePath.GetDirectory() / hpbName))
                    throw new FileNotFoundException($"{hpbName} not found.");

                hpiStream = await fileSystem.OpenFileAsync(filePath);
                hpbStream = await fileSystem.OpenFileAsync(filePath.GetDirectory() / hpbName);
            }
            else
            {
                var hpiName = filePath.GetNameWithoutExtension() + ".HPI";

                if (!fileSystem.FileExists(filePath.GetDirectory() / hpiName))
                    throw new FileNotFoundException($"{hpiName} not found.");

                hpiStream = await fileSystem.OpenFileAsync(filePath.GetDirectory() / hpiName);
                hpbStream = await fileSystem.OpenFileAsync(filePath);
            }

            _files = _hpiHpb.Load(hpiStream, hpbStream);
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream hpiStream, hpbStream;

            switch (savePath.GetExtensionWithDot())
            {
                case ".HPI":
                    hpiStream = fileSystem.OpenFile(savePath, FileMode.Create);
                    hpbStream = fileSystem.OpenFile(savePath.ChangeExtension("HPB"), FileMode.Create);
                    break;

                default:
                    hpiStream = fileSystem.OpenFile(savePath.ChangeExtension("HPI"), FileMode.Create);
                    hpbStream = fileSystem.OpenFile(savePath, FileMode.Create);
                    break;
            }

            _hpiHpb.Save(hpiStream, hpbStream, _files);

            return Task.CompletedTask;
        }

        public void ReplaceFile(IArchiveFile file, Stream fileData)
        {
            file.SetFileData(fileData);
        }
    }
}
