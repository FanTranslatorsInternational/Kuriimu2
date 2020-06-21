using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Progress;
using Kontract.Interfaces.Providers;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_tri_ace.Archives
{
    class PackState : IArchiveState, ILoadFiles
    {
        private readonly Pack _pack;

        public IReadOnlyList<ArchiveFileInfo> Files { get; private set; }

        public bool ContentChanged { get; set; }

        public PackState()
        {
            _pack = new Pack();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, ITemporaryStreamProvider temporaryStreamProvider,
            IProgressContext progress)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Files = _pack.Load(fileStream);
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, IProgressContext progress)
        {
            var fileStream = fileSystem.OpenFile(savePath, FileMode.Create);
            _pack.Save(fileStream, Files);

            return Task.CompletedTask;
        }
    }
}
