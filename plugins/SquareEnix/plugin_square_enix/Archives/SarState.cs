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

namespace plugin_square_enix.Archives
{
    class SarState : IArchiveState, ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private Sar _arc;

        public IList<IArchiveFileInfo> Files { get; private set; }
        public bool ContentChanged => IsContentChange();

        public SarState()
        {
            _arc = new Sar();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var dataStream = await fileSystem.OpenFileAsync(filePath);
            var matStream = await fileSystem.OpenFileAsync(filePath.ChangeExtension(".sar.mat"));

            Files = _arc.Load(dataStream, matStream);
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var dataStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);
            var matStream = fileSystem.OpenFile(savePath.ChangeExtension(".sar.mat"), FileMode.Create, FileAccess.Write);

            _arc.Save(dataStream, matStream, Files);

            return Task.CompletedTask;
        }


        public void ReplaceFile(IArchiveFileInfo afi, Stream fileData)
        {
            afi.SetFileData(fileData);
        }

        private bool IsContentChange()
        {
            return Files.Any(x => x.ContentChanged);
        }
    }
}
