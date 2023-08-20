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

namespace plugin_level5._3DS.Archives
{
    class B123State : IArchiveState, ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private readonly B123 _b123;
        private List<IArchiveFileInfo> _files;

        public IReadOnlyList<IArchiveFileInfo> Files { get; private set; }

        public bool ContentChanged => _files.Any(x => x.ContentChanged);

        public B123State()
        {
            _b123 = new B123();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Files = _files = _b123.Load(fileStream);
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = fileSystem.OpenFile(savePath, FileMode.Create);
            _b123.Save(fileStream, _files, saveContext.ProgressContext);

            return Task.CompletedTask;
        }

        public void ReplaceFile(IArchiveFileInfo afi, Stream fileData)
        {
            afi.SetFileData(fileData);
        }
    }
}
