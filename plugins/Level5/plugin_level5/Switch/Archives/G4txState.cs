using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Interfaces.Plugins.State.Features;
using Kontract.Models.FileSystem;
using Kontract.Models.Plugins.State;

namespace plugin_level5.Switch.Archives
{
    class G4txState : IArchiveState, ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private G4tx _g4tx;
        private List<IArchiveFileInfo> _files;

        public IReadOnlyList<IArchiveFileInfo> Files { get; private set; }
        public bool ContentChanged => _files.Any(x => x.ContentChanged);

        public G4txState()
        {
            _g4tx = new G4tx();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Files =_files= _g4tx.Load(fileStream);
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.ReadWrite);
            _g4tx.Save(fileStream, _files);

            return Task.CompletedTask;
        }

        public void ReplaceFile(IArchiveFileInfo afi, Stream fileData)
        {
            using var br = new BinaryReaderX(fileData, true);
            if (br.ReadString(5) != "NXTCH")
                throw new InvalidOperationException("File needs to be a valid NXTCH.");

            afi.SetFileData(fileData);
        }
    }
}
