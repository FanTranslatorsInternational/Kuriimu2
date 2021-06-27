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

namespace plugin_nintendo.Images
{
    public class BchPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("acd66c9c-da38-48e5-b614-2f4569775a5e");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.bch" };
        public PluginMetadata Metadata { get; }

        public BchPlugin()
        {
            Metadata = new PluginMetadata("BCH", "onepiecefreak", "The object resource for games on Nintendo 3DS.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(3) == "BCH";
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new BchState();
        }
    }
}
