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

namespace plugin_konami.Archives
{
    public class TarcPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("f7d52572-b076-4f0d-b7c2-533984428d20");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.tarc" };
        public PluginMetadata Metadata { get; }

        public TarcPlugin()
        {
            Metadata = new PluginMetadata("TARC", "onepiecefreak", "The main resource in Tongari Boushi.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "TBAF";
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new TarcState();
        }
    }
}
