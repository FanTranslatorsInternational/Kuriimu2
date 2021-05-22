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

namespace plugin_cattle_call.Images
{
    public class F3xtPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("36da51fd-c837-45e6-8350-1c295618bc2a");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.tex" };
        public PluginMetadata Metadata { get; }

        public F3xtPlugin()
        {
            Metadata = new PluginMetadata("F3XT", "onepiecefreak", "The image resource in Metal Max 4.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            var magic1 = br.ReadString(4);

            fileStream.Position++;
            var magic2 = br.ReadString(4);

            return magic1 == "F3XT" || magic2 == "F3XT";
        }

        public IPluginState CreatePluginState(IFileManager fileManager)
        {
            return new F3xtState();
        }
    }
}
