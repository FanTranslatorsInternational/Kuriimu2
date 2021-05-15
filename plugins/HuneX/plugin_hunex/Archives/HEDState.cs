using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.Archive;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace plugin_hunex.Archives
{
    class HEDState : IArchiveState, ILoadFiles
    {
        private HED _hed;

        public IList<IArchiveFileInfo> Files { get; private set; }

        public HEDState()
        {
            _hed = new HED();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var hedStream = await fileSystem.OpenFileAsync(filePath);
            var mrgStream = await fileSystem.OpenFileAsync(filePath.ChangeExtension("mrg"));

            Stream namStream = null;
            if (fileSystem.FileExists(filePath.ChangeExtension("nam")))
                namStream = await fileSystem.OpenFileAsync(filePath.ChangeExtension("nam"));

            Files = _hed.Load(hedStream, mrgStream, namStream);
        }
    }
}
