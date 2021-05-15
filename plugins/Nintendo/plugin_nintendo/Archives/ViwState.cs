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

namespace plugin_nintendo.Archives
{
    class ViwState : IArchiveState, ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private Viw _arc;

        public IList<IArchiveFileInfo> Files { get; private set; }
        public bool ContentChanged => IsContentChanged();

        public ViwState()
        {
            _arc = new Viw();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var viwStream = await fileSystem.OpenFileAsync(filePath);
            var infStream = await fileSystem.OpenFileAsync(filePath.ChangeExtension(".inf"));
            var dataStream = await fileSystem.OpenFileAsync(filePath.ChangeExtension(""));
            Files = _arc.Load(viwStream, infStream, dataStream);
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var viwStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);
            var infStream = fileSystem.OpenFile(savePath.ChangeExtension(".inf"), FileMode.Create, FileAccess.Write);
            var dataStream = fileSystem.OpenFile(savePath.ChangeExtension(""), FileMode.Create, FileAccess.Write);
            _arc.Save(viwStream, infStream, dataStream, Files);

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
