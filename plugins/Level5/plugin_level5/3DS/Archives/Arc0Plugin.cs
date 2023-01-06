using System;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers.Files;
using Kontract.Interfaces.Plugins.Entry;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.FileSystem;
using Kontract.Models.Plugins.Entry;

namespace plugin_level5._3DS.Archives
{
    public class Arc0Plugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("e75ba21c-f0f4-4d0e-8989-103ea2ac3cda");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.fa" };
        public PluginMetadata Metadata { get; }

        public Arc0Plugin()
        {
            Metadata = new PluginMetadata("ARC0", "onepiecefreak", "Main game archive for 3DS Level-5 games");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "ARC0";
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new Arc0State();
        }
    }
}
