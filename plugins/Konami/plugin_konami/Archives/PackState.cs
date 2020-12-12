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

namespace plugin_konami.Archives
{
    class PackState : IArchiveState, ILoadFiles
    {
        private Pack _arc;

        public IList<IArchiveFileInfo> Files { get; private set; }

        public PackState()
        {
            _arc = new Pack();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Files = _arc.Load(fileStream);
        }
    }
}
