using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Interfaces.Progress;
using Kontract.Interfaces.Providers;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_nintendo.Archives
{
    class NcchState : IArchiveState, ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private readonly NCCH _ncch;

        public IReadOnlyList<ArchiveFileInfo> Files { get; private set; }

        public bool ContentChanged { get; set; }

        public NcchState()
        {
            _ncch = new NCCH();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, ITemporaryStreamProvider temporaryStreamProvider,
            IProgressContext progress)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Files = _ncch.Load(fileStream);
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, IProgressContext progress)
        {
            var output = fileSystem.OpenFile(savePath, FileMode.Create);
            _ncch.Save(output, Files);

            return Task.CompletedTask;
        }

        public void ReplaceFile(ArchiveFileInfo afi, Stream fileData)
        {
            afi.SetFileData(fileData);
        }
    }
}
