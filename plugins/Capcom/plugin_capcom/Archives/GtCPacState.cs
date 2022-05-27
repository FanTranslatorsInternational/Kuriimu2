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
    public class GtCPacState: IArchiveState, ILoadFiles
    {
            private GtCPac _cpac;

            public IList<IArchiveFileInfo> Files { get; private set; }

            public GtCPacState()
            {
                _cpac = new GtCPac();
            }

            public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
            {
                var fileStream = await fileSystem.OpenFileAsync(filePath);
                Files = _cpac.Load(fileStream);
            }
        }

    
}
