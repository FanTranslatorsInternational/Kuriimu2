using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.Archive;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace plugin_nintendo.Archives
{
    class WiiDiscState : IArchiveState, ILoadFiles
    {
        private readonly WiiDisc _wiiDisc;

        public IList<ArchiveFileInfo> Files { get; private set; }

        public bool ContentChanged => IsChanged();

        public WiiDiscState()
        {
            _wiiDisc = new WiiDisc();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Files = _wiiDisc.Load(fileStream);
        }

        private bool IsChanged()
        {
            return Files.Any(x => x.ContentChanged);
        }
    }
}
