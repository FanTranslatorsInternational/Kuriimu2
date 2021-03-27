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

namespace plugin_mt_framework.Archives
{
    class HfsState : IArchiveState, ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private Hfs _hfs;

        public IList<IArchiveFileInfo> Files { get; private set; }
        public bool ContentChanged => IsContentChanged();

        public HfsState()
        {
            _hfs = new Hfs();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Files = _hfs.Load(fileStream, filePath.GetName());
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.ReadWrite);
            _hfs.Save(fileStream, Files);

            return Task.CompletedTask;
        }

        public void ReplaceFile(IArchiveFileInfo afi, Stream fileData)
        {
            afi.SetFileData(fileData);
        }

        public void Rename(IArchiveFileInfo afi, UPath path)
        {
            afi.FilePath = path;
        }

        private bool IsContentChanged()
        {
            return Files.Any(x => x.ContentChanged);
        }
    }
}
