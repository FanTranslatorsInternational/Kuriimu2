using System;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Providers;
using Kontract.Models;
using Kontract.Models.IO;

namespace plugin_nintendo.Archives
{
    public class CgrpPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("61553a57-c6bb-40fb-9c8d-c0e4425d29ee");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.cgrp" };
        public PluginMetadata Metadata { get; }

        public CgrpPlugin()
        {
            Metadata = new PluginMetadata("CGRP", "onepiecefreak", "One kind of archive in Nintendo games.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, ITemporaryStreamProvider temporaryStreamProvider)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "CGRP";
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new CgrpState();
        }
    }
}
