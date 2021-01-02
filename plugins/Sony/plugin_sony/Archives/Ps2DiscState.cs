using System.Collections.Generic;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.Archive;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace plugin_sony.Archives
{
    class Ps2DiscState : IArchiveState, ILoadFiles
    {
        private readonly Ps2Disc _ps2;

        public IList<IArchiveFileInfo> Files { get; private set; }

        public Ps2DiscState()
        {
            _ps2 = new Ps2Disc();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Files = _ps2.Load(fileStream);
        }
    }
}
