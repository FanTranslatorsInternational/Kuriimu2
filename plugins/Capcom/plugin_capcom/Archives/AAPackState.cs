using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Models;
using Kontract.Models.Archive;
using Kontract.Models.Context;
using Kontract.Models.Dialog;
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
                incStream = await fileSystem.OpenFileAsync(filePath);
                datStream = await fileSystem.OpenFileAsync(filePath.GetDirectory() / "pack.dat");
            }
            else
            {
                incStream = await fileSystem.OpenFileAsync(filePath.GetDirectory() / "pack.inc");
                datStream = await fileSystem.OpenFileAsync(filePath);
            }

            Files = _aatri.Load(incStream, datStream, AAPackSupport.GetVersion(loadContext.DialogManager));
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            throw new NotImplementedException();
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
