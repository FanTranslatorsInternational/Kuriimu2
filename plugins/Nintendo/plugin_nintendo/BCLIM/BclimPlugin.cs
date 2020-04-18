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

namespace plugin_nintendo.BCLIM
{
    public class BclimPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("cf5ae49f-0ce9-4241-900c-668b5c62ce33");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.bclim" };
        public PluginMetadata Metadata { get; }

        public BclimPlugin()
        {
            Metadata = new PluginMetadata("BCLIM", "IcySon55, onepiecefreak",
                "", "This is the BCLIM image adapter for Kuriimu.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, ITemporaryStreamProvider temporaryStreamProvider)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using (var br = new BinaryReaderX(fileStream))
            {
                if (br.BaseStream.Length < 0x28)
                    return false;

                br.BaseStream.Position = br.BaseStream.Length - 0x28;
                var magic = br.ReadString(4);
                br.BaseStream.Position += 8;

                return magic == "CLIM" && br.ReadInt32() == fileStream.Length;
            }
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new BclimState();
        }
    }
}
