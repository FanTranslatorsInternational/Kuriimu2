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

namespace plugin_bandai_namco.Archives
{
    class SegState : IArchiveState, ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private Seg _arc;

        public IList<IArchiveFileInfo> Files { get; private set; }
        public bool ContentChanged => IsContentChanged();

        public SegState()
        {
            _arc = new Seg();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var segStream = await fileSystem.OpenFileAsync(filePath);
            var binStream = await fileSystem.OpenFileAsync(filePath.ChangeExtension(".BIN"));

            var sizeName = filePath.GetDirectory() / filePath.GetNameWithoutExtension() + "SIZE.BIN";
            var sizeStream = fileSystem.FileExists(sizeName) ? await fileSystem.OpenFileAsync(sizeName) : null;

            Files = _arc.Load(segStream, binStream, sizeStream);
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var segStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);
            var binStream = fileSystem.OpenFile(savePath.ChangeExtension(".BIN"), FileMode.Create, FileAccess.Write);

            var sizeName = savePath.GetDirectory() / savePath.GetNameWithoutExtension() + "SIZE.BIN";
            var sizeStream = Files.Any(x => x.UsesCompression) ? fileSystem.OpenFile(sizeName, FileMode.Create, FileAccess.Write) : null;

            _arc.Save(segStream, binStream, sizeStream, Files);

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
