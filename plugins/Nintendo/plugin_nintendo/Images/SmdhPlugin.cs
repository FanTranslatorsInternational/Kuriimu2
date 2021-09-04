using System;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace plugin_nintendo.Images
{
    public class SmdhPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("3c977dce-d992-4eaf-ac17-0871408c68cf");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.bin" };
        public PluginMetadata Metadata { get; }

        public SmdhPlugin()
        {
            Metadata = new PluginMetadata("SMDH", "onepiecefreak", "The 3DS icon format.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "SMDH";
        }

        public IPluginState CreatePluginState(IBaseFileManager fileManager)
        {
            return new SmdhState();
        }
    }
}
