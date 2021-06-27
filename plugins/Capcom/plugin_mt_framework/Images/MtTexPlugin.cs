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

namespace plugin_mt_framework.Images
{
    public class MtTexPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("9e85ef16-7157-40ba-846a-b5a17148775f");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.tex" };
        public PluginMetadata Metadata { get; }

        public MtTexPlugin()
        {
            Metadata = new PluginMetadata("MT TEX", "onepiecefreak", "Main image resource for the MT Framework by Capcom.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            var magic = br.ReadString(4);
            return magic == "TEX\0" || magic == "\0XET" || magic == "TEX ";
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new MtTexState();
        }
    }
}
