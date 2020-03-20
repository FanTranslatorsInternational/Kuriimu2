using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Providers;
using Kontract.Models;
using Kontract.Models.IO;

namespace plugin_level5.Archives
{
    public class Arc0Plugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("e75ba21c-f0f4-4d0e-8989-103ea2ac3cda");
        public string[] FileExtensions => new[] { "*.fa" };
        public PluginMetadata Metadata { get; }

        public Arc0Plugin()
        {
            Metadata = new PluginMetadata("ARC0", "onepiecefreak", "Main game archive for 3DS Level-5 games");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, ITemporaryStreamProvider temporaryStreamProvider)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "ARC0";
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new Arc0State();
        }
    }
}
