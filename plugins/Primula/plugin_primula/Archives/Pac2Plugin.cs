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

namespace plugin_primula.Archives
{
    public class Pac2Plugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("AF5ADDBD-BF3A-4168-A287-BD78C9306DEB");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.dat" };
        public PluginMetadata Metadata { get; }

        public Pac2Plugin()
        {
            Metadata = new PluginMetadata("Pac2", "Megaflan", "The main archive resource in Primula games.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(12) == "GAMEDAT PAC2";
        }

        public IPluginState CreatePluginState(IBaseFileManager fileManager)
        {
            return new Pac2State();
        }
    }
}
