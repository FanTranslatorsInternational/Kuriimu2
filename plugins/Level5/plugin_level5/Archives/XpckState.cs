using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Interfaces.Progress;
using Kontract.Interfaces.Providers;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_level5.Archives
{
    public class XpckState : IArchiveState, ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private readonly Xpck _xpck;

        public IReadOnlyList<ArchiveFileInfo> Files { get; private set; }
        public bool ContentChanged { get; set; }

        public XpckState()
        {
            _xpck = new Xpck();
        }

        public async void Load(IFileSystem fileSystem, UPath filePath, ITemporaryStreamProvider temporaryStreamProvider,
            IProgressContext progress)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Files = _xpck.Load(fileStream);
        }

        public void Save(IFileSystem fileSystem, UPath savePath, IProgressContext progress)
        {
            var output = fileSystem.OpenFile(savePath, FileMode.Create);
            _xpck.Save(output, Files);
        }

        public void ReplaceFile(ArchiveFileInfo afi, Stream fileData)
        {
            afi.SetFileData(fileData);
        }
    }
}
