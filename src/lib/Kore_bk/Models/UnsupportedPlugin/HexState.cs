using System.IO;
using System.Threading.Tasks;
using Kontract.Interfaces.Plugins.State.Features;
using Kontract.Models.Plugins.State;
using Zio;

namespace Kore.Models.UnsupportedPlugin
{
    public class HexState : IHexState, ILoadFiles
    {
        public Stream FileStream { get; private set; }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            FileStream = await fileSystem.OpenFileAsync(filePath);
        }
    }
}
