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

namespace most_wanted_ent.Images
{
    public class CtgdPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("68b01e10-af37-4064-bd14-1bdcd10036ff");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] {"*.tgd", "*.ctgd"};
        public PluginMetadata Metadata { get; }

        public CtgdPlugin()
        {
            Metadata = new PluginMetadata("CTGD", "onepiecefreak", "The image resource in Memory Tales Time Travel.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);

            fileStream.Position = 4;
            var magic = br.ReadString(4);

            fileStream.Position = 9;
            var magic1 = br.ReadString(4);

            return magic == "nns_" || magic1 == "nns_";
        }

        public IPluginState CreatePluginState(IBaseFileManager fileManager)
        {
            return new CtgdState();
        }
    }
}
