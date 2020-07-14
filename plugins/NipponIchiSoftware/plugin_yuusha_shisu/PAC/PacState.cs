using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.Archive;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace plugin_yuusha_shisu.PAC
{
    public class PacState : IArchiveState, ILoadFiles, ISaveFiles
    {
        private readonly Pac _pac;

        public IReadOnlyList<ArchiveFileInfo> Files { get; private set; }

        public bool ContentChanged { get; set; }

        public PacState()
        {
            _pac = new Pac();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Files = _pac.Load(fileStream);
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var saveStream = fileSystem.OpenFile(savePath, FileMode.Create);
            _pac.Save(saveStream, Files);

            return Task.CompletedTask;
        }
    }
}
