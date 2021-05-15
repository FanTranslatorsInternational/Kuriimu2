using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Models.Archive;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace plugin_ganbarion.Archives
{
    class JarcState : IArchiveState, ILoadFiles, ISaveFiles, IReplaceFiles
    {
        private string _magic;
        private Jarc _jarc;
        private Jcmp _jcmp;

        public IList<IArchiveFileInfo> Files { get; private set; }
        public bool ContentChanged => IsContentChanged();

        public JarcState()
        {
            _jarc = new Jarc();
            _jcmp = new Jcmp();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream, true);
            _magic = br.PeekString();

            switch (_magic)
            {
                case "jARC":
                    Files = _jarc.Load(fileStream);
                    break;

                case "jCMP":
                    Files = _jcmp.Load(fileStream);
                    break;
            }
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);

            switch (_magic)
            {
                case "jARC":
                    _jarc.Save(fileStream, Files);
                    break;

                case "jCMP":
                    _jcmp.Save(fileStream, Files);
                    break;
            }

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
