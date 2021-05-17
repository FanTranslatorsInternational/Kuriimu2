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

namespace plugin_koei_tecmo.Archives
{
    class IdxState : IArchiveState, ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private Idx _arc;

        public IList<IArchiveFileInfo> Files { get; private set; }
        public bool ContentChanged => IsContentChanged();

        public IdxState()
        {
            _arc = new Idx();
        }

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

            Files = _arc.Load(idxStream, binStream);
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream idxStream;
            Stream binStream;

            if (savePath.GetExtensionWithDot() == ".idx")
            {
                idxStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);
                binStream = fileSystem.OpenFile(savePath.ChangeExtension(".bin"), FileMode.Create, FileAccess.Write);
            }
            else
            {
                idxStream = fileSystem.OpenFile(savePath.ChangeExtension(".idx"), FileMode.Create, FileAccess.Write);
                binStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);
            }

            _arc.Save(idxStream, binStream, Files);

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
