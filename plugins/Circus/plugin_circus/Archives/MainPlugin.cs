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

namespace plugin_circus.Archives
{
    public class MainPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("fa182181-76f5-4b7c-aebf-c8466c01aa1e");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.bin" };
        public PluginMetadata Metadata { get; }

        public MainPlugin()
        {
            Metadata = new PluginMetadata("Da Capo Script", "onepiecefreak", "The script file of Da Capo games.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            var magic = br.ReadString(3);
            return magic == "DC1" || magic == "DC2";
        }

        public IPluginState CreatePluginState(IBaseFileManager pluginManager)
        {
            return new MainState();
        }
    }
}
