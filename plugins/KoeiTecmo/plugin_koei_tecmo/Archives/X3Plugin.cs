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

namespace plugin_koei_tecmo.Archives
{
    public class X3Plugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("68d4c5dd-ff62-43a5-a904-b550fe00a37d");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.bin" };
        public PluginMetadata Metadata { get; }

        public X3Plugin()
        {
            Metadata = new PluginMetadata("X3", "onepiecefreak", "The main resource package in KoeiTecmo games.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadInt32() == 0x0133781D;
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new X3State();
        }
    }
}
