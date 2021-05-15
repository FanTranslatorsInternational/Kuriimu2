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
    public class GcBnrPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("50ec8196-b1ff-4d2d-85e8-f56e557ba9c2");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.bnr", "*.bin" };
        public PluginMetadata Metadata { get; }

        public GcBnrPlugin()
        {
            Metadata = new PluginMetadata("BNR", "onepiecefreak", "The GameCube Banner format.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            var magic = br.ReadString(4);
            return magic == "BNR1" || magic == "BNR2";
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new GcBnrState();
        }
    }
}
