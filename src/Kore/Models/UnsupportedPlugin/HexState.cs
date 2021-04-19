using System.IO;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace Kore.Models.UnsupportedPlugin
{
    public class HexState : IHexState
    {
        public Stream FileStream { get; private set; }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            FileStream = await fileSystem.OpenFileAsync(filePath);
        }
    }
}
