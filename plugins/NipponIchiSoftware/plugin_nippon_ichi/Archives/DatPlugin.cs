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

namespace plugin_nippon_ichi.Archives
{
    public class DatPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("89c7658b-6371-4be9-96b4-db9b9eb77be9");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.DAT" };
        public PluginMetadata Metadata { get; }

        public DatPlugin()
        {
            Metadata = new PluginMetadata("DAT", "onepiecefreak", "Main resource in Hayarigami games.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(7) == "NISPACK";
        }

        public IPluginState CreatePluginState(IFileManager fileManager)
        {
            return new DatState();
        }
    }
}
