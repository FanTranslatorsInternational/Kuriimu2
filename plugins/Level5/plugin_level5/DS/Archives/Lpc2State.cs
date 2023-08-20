using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Interfaces.Plugins.State.Features;
using Kontract.Models.FileSystem;
using Kontract.Models.Plugins.State;

namespace plugin_level5.DS.Archives
{
    class Lpc2State : IArchiveState, ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private readonly Lpc2 _lpc2;
        private List<IArchiveFileInfo> _files;

        public IReadOnlyList<IArchiveFileInfo> Files { get; private set; }

        public bool ContentChanged => _files.Any(x => x.ContentChanged);

        public Lpc2State()
        {
            _lpc2 = new Lpc2();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Files =_files = _lpc2.Load(fileStream);
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = fileSystem.OpenFile(savePath, FileMode.Create);
            _lpc2.Save(fileStream, _files);

            return Task.CompletedTask;
        }

        public void ReplaceFile(IArchiveFileInfo afi, Stream fileData)
        {
            afi.SetFileData(fileData);
        }
    }
}
