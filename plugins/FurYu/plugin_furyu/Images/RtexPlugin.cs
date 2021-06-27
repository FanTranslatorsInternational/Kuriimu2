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

namespace plugin_alchemist.Images
{
    public class RtexPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("4dbf4d5b-ae1d-4369-b02d-295f93fac10c");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.rtex" };
        public PluginMetadata Metadata { get; }

        public RtexPlugin()
        {
            Metadata = new PluginMetadata("RTEX", "onepiecefreak", "The main image resource in Gaki no Tsukai.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "RTEX";
        }

        public IPluginState CreatePluginState(IFileManager fileManager)
        {
            return new RtexState();
        }
    }
}
