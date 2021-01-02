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

namespace plugin_ruby_party.Archives
{
    public class PaaPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("cd294c2b-964c-4a05-8a8b-36387d8f8bc7");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.bin" };
        public PluginMetadata Metadata { get; }

        public PaaPlugin()
        {
            Metadata = new PluginMetadata("PAA", "onepiecefreak", "The main archive in Angelique Retour.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(3) == "PAA";
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new PaaState();
        }
    }
}
