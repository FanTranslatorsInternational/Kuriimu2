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

namespace plugin_square_enix.Archives
{
    public class PackPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("00d5ca3f-419f-4426-bea3-168159fe28db");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.pack" };
        public PluginMetadata Metadata { get; }

        public PackPlugin()
        {
            Metadata = new PluginMetadata("PACK", "onepiecefreak", "The resource archive for Dragon Quest XI.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            var magic = br.ReadString(4);
            return magic == "PACK" || magic == "PACA";
        }

        public IPluginState CreatePluginState(IBaseFileManager pluginManager)
        {
            return new PackState();
        }
    }
}
