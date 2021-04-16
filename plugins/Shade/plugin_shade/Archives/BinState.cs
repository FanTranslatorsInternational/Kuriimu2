using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Models.Archive;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace plugin_shade.Archives
{
    class BinState : IArchiveState, ILoadFiles
    {
        private readonly Bin _shBin;

        public IList<IArchiveFileInfo> Files { get; private set; }

        public BinState() 
        {
            _shBin = new Bin();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Files = _shBin.Load(fileStream);
        }

    }
}
