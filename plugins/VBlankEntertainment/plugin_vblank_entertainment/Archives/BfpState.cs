using System.Collections.Generic;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.Archive;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace plugin_vblank_entertainment.Archives
{
    class BfpState : IArchiveState, ILoadFiles
    {
        private Bfp _bfp;

        public IList<ArchiveFileInfo> Files { get; private set; }

        public BfpState()
        {
            _bfp = new Bfp();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Files = _bfp.Load(fileStream);
        }
    }
}
