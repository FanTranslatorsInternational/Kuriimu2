using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace plugin_hunex.Archives
{
    public class MRGPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("a2f60c9b-5c70-4415-80c3-50c967ae4ebb");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.mrg", "*.mzp" };
        public PluginMetadata Metadata { get; }

        public MRGPlugin()
        {
            Metadata = new PluginMetadata("MRG", "Sn0wcrack; onepiecefreak", "The second main archive of HuneX games.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(6) == "mrgd00";
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new MRGState();
        }
    }
}
