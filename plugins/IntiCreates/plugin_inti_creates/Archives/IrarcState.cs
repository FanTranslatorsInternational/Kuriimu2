using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Models.Archive;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace plugin_inti_creates.Archives
{
    class IrarcState : IArchiveState, ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private Irarc _irarc;

        public IList<ArchiveFileInfo> Files { get; private set; }

        public bool ContentChanged => IsContentChanged();

        public IrarcState()
        {
            _irarc = new Irarc();
        }

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

            Files = _irarc.Load(lstStream, arcStream);
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream lstStream;
            Stream arcStream;

            var lstName = $"{savePath.GetNameWithoutExtension()}.irlst";
            var arcName = $"{savePath.GetNameWithoutExtension()}.irarc";

            switch (savePath.GetExtensionWithDot())
            {
                case ".irlst":
                    lstStream = fileSystem.OpenFile(savePath.GetDirectory() / lstName, FileMode.Create);
                    arcStream = fileSystem.OpenFile(savePath.GetDirectory() / arcName, FileMode.Create);
                    break;

                default:
                    lstStream = fileSystem.OpenFile(savePath.GetDirectory() / lstName, FileMode.Create);
                    arcStream = fileSystem.OpenFile(savePath.GetDirectory() / arcName, FileMode.Create);
                    break;
            }

            _irarc.Save(lstStream, arcStream, Files);

            return Task.CompletedTask;
        }

        public void ReplaceFile(ArchiveFileInfo afi, Stream fileData)
        {
            afi.SetFileData(fileData);
        }

        private bool IsContentChanged()
        {
            return Files.Any(x => x.ContentChanged);
        }
    }
}
