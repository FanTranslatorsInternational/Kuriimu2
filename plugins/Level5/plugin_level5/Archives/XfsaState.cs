using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Interfaces.Progress;
using Kontract.Interfaces.Providers;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_level5.Archives
{
    class XfsaState : IArchiveState, ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private readonly Xfsa _xfsa;

        public IReadOnlyList<ArchiveFileInfo> Files { get; private set; }
        public bool ContentChanged => IsChanged();

        public XfsaState()
        {
            _xfsa = new Xfsa();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, ITemporaryStreamProvider temporaryStreamProvider,
            IProgressContext progress)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Files = _xfsa.Load(fileStream);
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, IProgressContext progress)
        {
            var output = fileSystem.OpenFile(savePath, FileMode.Create);
            _xfsa.Save(output, Files, progress);

            return Task.CompletedTask;
        }

        public void ReplaceFile(ArchiveFileInfo afi, Stream fileData)
        {
            afi.SetFileData(fileData);
        }

        private bool IsChanged()
        {
            return Files.Any(x => x.ContentChanged);
        }
    }
}
