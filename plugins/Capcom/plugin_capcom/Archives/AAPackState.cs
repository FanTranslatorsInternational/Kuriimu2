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

namespace plugin_capcom.Archives
{
    class AAPackState : IArchiveState, ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private readonly AAPack _aatri;

        public IList<ArchiveFileInfo> Files { get; private set; }

        public bool ContentChanged => IsContentChanged();

        public AAPackState()
        {
            _aatri = new AAPack();
        }

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

            Files = _aatri.Load(incStream, datStream, AAPackSupport.GetVersion(loadContext.DialogManager));
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream incStream;
            Stream datStream;

            switch (savePath.GetExtensionWithDot())
            {
                case ".inc":
                    incStream = fileSystem.OpenFile(savePath.GetDirectory() / "pack.inc", FileMode.Create);
                    datStream = fileSystem.OpenFile(savePath.GetDirectory() / savePath.GetNameWithoutExtension() + ".dat", FileMode.Create);
                    break;

                default:
                    incStream = fileSystem.OpenFile(savePath.GetDirectory() / savePath.GetNameWithoutExtension() + ".inc", FileMode.Create);
                    datStream = fileSystem.OpenFile(savePath.GetDirectory() / "pack.dat", FileMode.Create);
                    break;
            }

            _aatri.Save(incStream, datStream, Files);

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
