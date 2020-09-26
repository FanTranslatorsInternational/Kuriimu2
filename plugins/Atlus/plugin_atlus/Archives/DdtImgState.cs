using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Models.Archive;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace plugin_atlus.Archives
{
    class DdtImgState : IArchiveState, ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private DdtImg _ddtImg;

        public IList<IArchiveFileInfo> Files { get; private set; }
        public bool ContentChanged => IsContentChanged();

        public DdtImgState()
        {
            _ddtImg = new DdtImg();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream imgStream;
            Stream ddtStream;

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

            Files = _ddtImg.Load(ddtStream, imgStream);
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream imgStream;
            Stream ddtStream;

            switch (savePath.GetExtensionWithDot())
            {
                case ".IMG":
                    var ddtPath = savePath.GetDirectory() / (savePath.GetNameWithoutExtension() + ".DDT");
                    imgStream = fileSystem.OpenFile(savePath, FileMode.Create);
                    ddtStream = fileSystem.OpenFile(ddtPath, FileMode.Create);
                    break;

                default:
                    var imgPath = savePath.GetDirectory() / (savePath.GetNameWithoutExtension() + ".IMG");
                    imgStream = fileSystem.OpenFile(imgPath, FileMode.Create);
                    ddtStream = fileSystem.OpenFile(savePath, FileMode.Create);
                    break;
            }

            _ddtImg.Save(ddtStream, imgStream, Files);

            return Task.CompletedTask;
        }

        public void ReplaceFile(IArchiveFileInfo afi, Stream fileData)
        {
            afi.SetFileData(fileData);
        }

        private bool IsContentChanged()
        {
            return Files.Any(x => x.ContentChanged);
        }
    }
}
