using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Models.Archive;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace plugin_shade.Archives
{
    class BinState : IArchiveState, ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private readonly Bin _bin;

        public IList<IArchiveFileInfo> Files { get; private set; }

        public bool ContentChanged => IsChanged();

        public BinState() 
        {
            _bin = new Bin();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Files = _bin.Load(fileStream);
        }
        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = fileSystem.OpenFile(savePath, FileMode.Create);
            _bin.Save(fileStream, Files);

            return Task.CompletedTask;
        }


        public void ReplaceFile(IArchiveFileInfo afi, Stream fileData)
        {
            afi.SetFileData(fileData);
        }
        private bool IsChanged()
        {
            return Files.Any(x => x.ContentChanged);
        }

    }
}
