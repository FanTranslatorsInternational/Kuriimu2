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
    public class XpckState : IArchiveState, ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private readonly Xpck _xpck;
        private List<IArchiveFileInfo> _files;

        public IReadOnlyList<IArchiveFileInfo> Files { get; private set; }
        public bool ContentChanged => _files.Any(x => x.ContentChanged);

        public XpckState()
        {
            _xpck = new Xpck();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Files = _files = _xpck.Load(fileStream);
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var output = fileSystem.OpenFile(savePath, FileMode.Create);
            _xpck.Save(output, _files);

            return Task.CompletedTask;
        }

        public void ReplaceFile(IArchiveFileInfo afi, Stream fileData)
        {
            afi.SetFileData(fileData);
        }
    }
}
