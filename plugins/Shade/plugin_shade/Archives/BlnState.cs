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

namespace plugin_shade.Archives
{
    class BlnState : IArchiveState, ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private readonly Bln _bln;

        public IList<IArchiveFileInfo> Files { get; private set; }

        public bool ContentChanged => IsChanged();

        public BlnState()
        {
            _bln = new Bln();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream dataStream;
            Stream indexStream;

            switch (filePath.GetName())
            {
                case "mcb1.bln":
                    dataStream = await fileSystem.OpenFileAsync(filePath);
                    indexStream = await fileSystem.OpenFileAsync(filePath.GetDirectory() / "mcb0.bln");
                    break;

                default:
                    indexStream = await fileSystem.OpenFileAsync(filePath);
                    dataStream = await fileSystem.OpenFileAsync(filePath.GetDirectory() / "mcb1.bln");
                    break;
            }

            if (dataStream == null || indexStream == null)
                throw new InvalidOperationException("This is no Bln archive.");

            Files = _bln.Load(indexStream, dataStream);
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream dataOutput;
            Stream indexOutput;

            switch (savePath.GetName())
            {
                case "mcb1.bln":
                    dataOutput = fileSystem.OpenFile(savePath, FileMode.Create);
                    indexOutput = fileSystem.OpenFile(savePath.GetDirectory() / "mcb0.bln", FileMode.Create);
                    break;

                default:
                    indexOutput = fileSystem.OpenFile(savePath, FileMode.Create);
                    dataOutput = fileSystem.OpenFile(savePath.GetDirectory() / "mcb1.bln", FileMode.Create);
                    break;
            }

            _bln.Save(indexOutput, dataOutput, Files);

            return Task.CompletedTask;
        }

        public void ReplaceFile(IArchiveFileInfo afi, Stream fileData)
        {
            afi.SetFileData(fileData);
        }

        private bool IsChanged()
        {
            return Files.Any(x => x.ContentChanged);
        }
    }
}
