using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;
using Kryptography.Encryption;
using System.IO;

namespace plugin_atlus.PS2.Archive
{
    class DdtImgState : ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private DdtImg _ddtImg = new();

        public List<ArchiveFileInfo> _files;

        public IReadOnlyList<IArchiveFile> Files => (IReadOnlyList<IArchiveFile>)_files;

        public bool ContentChanged => _files.Any(x => x.ContentChanged);

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream imgStream, ddtStream;

            if (filePath.GetExtensionWithDot() == ".IMG")
            {
                var ddtPath = filePath.GetDirectory() / (filePath.GetNameWithoutExtension() + ".DDT");
                if (!fileSystem.FileExists(ddtPath))
                    throw new FileNotFoundException($"{ddtPath.GetName()} not found.");

                imgStream = await fileSystem.OpenFileAsync(filePath);
                ddtStream = await fileSystem.OpenFileAsync(ddtPath);
            }
            else
            {
                var imgPath = filePath.GetDirectory() / (filePath.GetNameWithoutExtension() + ".IMG");
                if (!fileSystem.FileExists(imgPath))
                    throw new FileNotFoundException($"{imgPath.GetName()} not found.");

                imgStream = await fileSystem.OpenFileAsync(imgPath);
                ddtStream = await fileSystem.OpenFileAsync(filePath);
            }
            _files = _ddtImg.Load(ddtStream, imgStream);
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream imgStream, ddtStream;

            if (savePath.GetExtensionWithDot() == ".IMG")
            {
                var ddtPath = savePath.GetDirectory() / (savePath.GetNameWithoutExtension() + ".DDT");
                imgStream = fileSystem.OpenFile(savePath, FileMode.Create);
                ddtStream = fileSystem.OpenFile(ddtPath, FileMode.Create);
            }
            else
            {
                var imgPath = savePath.GetDirectory() / (savePath.GetNameWithoutExtension() + ".IMG");
                imgStream = fileSystem.OpenFile(imgPath, FileMode.Create);
                ddtStream = fileSystem.OpenFile(savePath, FileMode.Create);
            }
            _ddtImg.Save(ddtStream, imgStream, (List<ArchiveFile>)Files);
        }

        public void ReplaceFile(IArchiveFile file, Stream fileData)
        {
            file.SetFileData(fileData);
        }
    }
}
