using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace Atlus.Archives
{
    public class HpiHpbPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("9479a384-5725-47c0-9257-0f3f88fdbcde");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.hpi", "*.hpb" };
        public PluginMetadata Metadata { get; }

        public HpiHpbPlugin()
        {
            Metadata = new PluginMetadata("HPIHPB", "onepiecefreak", "The main archive for Etrian Odyssey games on 3DS.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var hpiPath = filePath;
            if (filePath.GetExtensionWithDot() == ".HPB")
                hpiPath = filePath.ChangeExtension("HPI");

            if (!fileSystem.FileExists(hpiPath))
                return false;

            var fileStream = await fileSystem.OpenFileAsync(hpiPath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "HPIH";
        }

        public IPluginState CreatePluginState(IBaseFileManager pluginManager)
        {
            return new HpiHpbState();
        }
    }
}
