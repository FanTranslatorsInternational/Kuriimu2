using System.IO;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Features;
using Kontract.Models.FileSystem;
using Kontract.Models.Plugins.State;

namespace Kore.Models.UnsupportedPlugin
{
    public class RawState : IRawState, ILoadFiles
    {
        public Stream FileStream { get; private set; }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            FileStream = await fileSystem.OpenFileAsync(filePath);
        }
    }
}
