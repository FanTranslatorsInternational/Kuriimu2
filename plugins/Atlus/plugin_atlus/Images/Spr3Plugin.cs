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

namespace plugin_atlus.Images
{
    public class Spr3Plugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("e8df5de0-39a7-4bbe-9779-8fd687da0fe7");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] {"*.spr3"};
        public PluginMetadata Metadata { get; }

        public Spr3Plugin()
        {
            Metadata=new PluginMetadata("SPR3","onepiecefreak","The main image resource in Persona Q games.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br=new BinaryReaderX(fileStream);
            fileStream.Position = 8;

            return br.ReadString(4) == "SPR3";
        }

        public IPluginState CreatePluginState(IBaseFileManager fileManager)
        {
            return new Spr3State(fileManager);
        }
    }
}
