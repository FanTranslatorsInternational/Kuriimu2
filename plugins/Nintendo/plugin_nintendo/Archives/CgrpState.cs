using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Interfaces.Progress;
using Kontract.Interfaces.Providers;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_nintendo.Archives
{
    class CgrpState : IArchiveState, ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private readonly Cgrp _cgrp;

        public IReadOnlyList<ArchiveFileInfo> Files { get; private set; }

        public bool ContentChanged { get; set; }

        public CgrpState()
        {
            _cgrp = new Cgrp();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, ITemporaryStreamProvider temporaryStreamProvider,
            IProgressContext progress)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Files = _cgrp.Load(fileStream);
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, IProgressContext progress)
        {
            var output = fileSystem.OpenFile(savePath, FileMode.Create);
            _cgrp.Save(output, Files);

            return Task.CompletedTask;
        }

        public void ReplaceFile(ArchiveFileInfo afi, Stream fileData)
        {
            afi.SetFileData(fileData);
        }
    }
}
