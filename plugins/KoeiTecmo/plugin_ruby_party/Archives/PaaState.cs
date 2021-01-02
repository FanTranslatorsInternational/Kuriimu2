using System;
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

namespace plugin_ruby_party.Archives
{
    class PaaState : IArchiveState, ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private Paa _arc;

        public IList<IArchiveFileInfo> Files { get; private set; }
        public bool ContentChanged => IsContentChanged();

        public PaaState()
        {
            _arc = new Paa();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            var arcName = filePath.ChangeExtension(".arc");
            var arcStream = await fileSystem.OpenFileAsync(arcName);

            Files = _arc.Load(fileStream, arcStream);
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);

            var arcName = savePath.ChangeExtension(".arc");
            var arcStream = fileSystem.OpenFile(arcName, FileMode.Create, FileAccess.Write);

            _arc.Save(fileStream, arcStream, Files);

            return Task.CompletedTask;
        }

        public void ReplaceFile(IArchiveFileInfo afi, Stream fileData)
        {
            afi.SetFileData(fileData);
        }

        private bool IsContentChanged()
        {
            return Files.Any(x => x.ContentChanged);
        }
    }
}
