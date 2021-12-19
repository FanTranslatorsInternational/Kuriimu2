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

namespace plugin_criware.Archives
{
    class CpkState : IArchiveState, ILoadFiles, ISaveFiles, IReplaceFiles, IRemoveFiles
    {
        private readonly Cpk _cpk;
        private bool _filesDeleted;

        public IList<IArchiveFileInfo> Files { get; private set; }

        public bool ContentChanged => IsContentChanged();

        public CpkState()
        {
            _cpk = new Cpk();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            _filesDeleted = false;

            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Files = _cpk.Load(fileStream);
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);
            _cpk.Save(fileStream, Files);

            return Task.CompletedTask;
        }

        public void ReplaceFile(IArchiveFileInfo afi, Stream fileData)
        {
            afi.SetFileData(fileData);
        }

        private bool IsContentChanged()
        {
            return Files.Any(x => x.ContentChanged) ||_filesDeleted;
        }

        public void RemoveFile(IArchiveFileInfo afi)
        {
            Files.Remove(afi);
            _cpk.DeleteFile(afi);

            _filesDeleted = true;
        }

        public void RemoveAll()
        {
            Files.Clear();
            _cpk.DeleteAll();

            _filesDeleted = true;
        }
    }
}
