using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Models.Archive;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace Atlus.Archives
{
    class HpiHpbState : IArchiveState, ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private HpiHpb _hpiHpb;

        public IList<ArchiveFileInfo> Files { get; private set; }

        public bool ContentChanged => IsContentChanged();

        public HpiHpbState()
        {
            _hpiHpb = new HpiHpb();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream hpiStream;
            Stream hpbStream;

            if (filePath.GetExtensionWithDot() == ".HPI")
            {
                var hpbName = filePath.GetNameWithoutExtension() + ".HPB";

                if (!fileSystem.FileExists(filePath.GetDirectory() / hpbName))
                    throw new FileNotFoundException($"{hpbName} not found.");

                hpiStream = await fileSystem.OpenFileAsync(filePath);
                hpbStream = await fileSystem.OpenFileAsync(filePath.GetDirectory() / hpbName);
            }
            else
            {
                var hpiName = filePath.GetNameWithoutExtension() + ".HPI";

                if (!fileSystem.FileExists(filePath.GetDirectory() / hpiName))
                    throw new FileNotFoundException($"{hpiName} not found.");

                hpiStream = await fileSystem.OpenFileAsync(filePath.GetDirectory() / hpiName);
                hpbStream = await fileSystem.OpenFileAsync(filePath);
            }

            Files = _hpiHpb.Load(hpiStream, hpbStream);
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream hpiStream;
            Stream hpbStream;

            switch (savePath.GetExtensionWithDot())
            {
                case ".HPI":
                    hpiStream = fileSystem.OpenFile(savePath, FileMode.Create);
                    hpbStream = fileSystem.OpenFile(savePath.ChangeExtension("HPB"), FileMode.Create);
                    break;

                default:
                    hpiStream = fileSystem.OpenFile(savePath.ChangeExtension("HPI"), FileMode.Create);
                    hpbStream = fileSystem.OpenFile(savePath, FileMode.Create);
                    break;
            }

            _hpiHpb.Save(hpiStream, hpbStream, Files);

            return Task.CompletedTask;
        }

        public void ReplaceFile(ArchiveFileInfo afi, Stream fileData)
        {
            afi.SetFileData(fileData);
        }

        private bool IsContentChanged()
        {
            return Files.Any(x => x.ContentChanged);
        }
    }
}
