using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.Archive;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace plugin_spike_chunsoft.Archives
{
    class NonaryMainState:IArchiveState,ILoadFiles
    {
        private NonaryMain _arc;

        public IList<IArchiveFileInfo> Files { get; private set; }

        public NonaryMainState()
        {
            _arc=new NonaryMain();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Files = _arc.Load(fileStream);
        }
    }
}
