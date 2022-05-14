using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.Archive;
using Kontract.Models.Context;
using Kontract.Models.IO;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace plugin_capcom.Archives
{
    public class GtPacState: IArchiveState, ILoadFiles
    {
            private GtPac _pac;

            public IList<IArchiveFileInfo> Files { get; private set; }

            public GtPacState()
            {
                _pac = new GtPac();
            }

            public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
            {
                var fileStream = await fileSystem.OpenFileAsync(filePath);
                Files = _pac.Load(fileStream);
            }
        }

    
}
