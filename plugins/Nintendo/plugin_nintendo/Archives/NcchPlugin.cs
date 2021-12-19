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

namespace plugin_nintendo.Archives
{
    public class NcchPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("7d0177a6-1cab-44b3-bf22-39f5548d6cac");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.cxi", "*.cfa" };
        public PluginMetadata Metadata { get; }

        public NcchPlugin()
        {
            Metadata = new PluginMetadata("NCCH", "onepiecefreak", "3DS Content Container.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br=new BinaryReaderX(fileStream);

            fileStream.Position = 0x100;
            return br.ReadString(4) == "NCCH";
        }

        public IPluginState CreatePluginState(IBaseFileManager pluginManager)
        {
            return new NcchState();
        }
    }
}
