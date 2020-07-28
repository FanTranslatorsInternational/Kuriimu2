using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Models.Archive;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace plugin_level5._3DS.Archives
{
    class XfsaState : IArchiveState, ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private readonly Xfsa _xfsa;

        public IList<ArchiveFileInfo> Files { get; private set; }
        public bool ContentChanged => IsChanged();

        public XfsaState()
        {
            _xfsa = new Xfsa();
        }

        public async void Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Files = _xfsa.Load(fileStream);
        }

        public void Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var output = fileSystem.OpenFile(savePath, FileMode.Create);
            _xfsa.Save(output, Files, saveContext.ProgressContext);
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
