using System;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers.Files;
using Kontract.Interfaces.Plugins.Entry;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.FileSystem;
using Kontract.Models.Plugins.Entry;

namespace plugin_level5.Mobile.Archives
{
    public class Hp10Plugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("1a2aec5f-568c-43e8-8fa0-7178ded1a39d");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.obb" };
        public PluginMetadata Metadata { get; }

        public Hp10Plugin()
        {
            Metadata = new PluginMetadata("HP10", "onepiecefreak", "Main data of Lady Layton on Android.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br=new BinaryReaderX(fileStream);
            return br.ReadString(4) == "HP10";
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new Hp10State();
        }
    }
}
