using System;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers.Files;
using Kontract.Interfaces.Plugins.Entry;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.FileSystem;
using Kontract.Models.Plugins.Entry;

namespace plugin_level5.DS.Images
{
    public class GtxtPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("20341149-76dc-43a5-9c02-d87b16f8b369");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.lt", "*.lp" };
        public PluginMetadata Metadata { get; }

        public GtxtPlugin()
        {
            Metadata = new PluginMetadata("GTXT", "onepiecefreak", "The main image resource in Professor Layton Spectre's Call by Level5.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br=new BinaryReaderX(fileStream);
            var magic = br.ReadString(4);

            return magic == "GTXT" || magic == "GPLT";
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new GtxtState();
        }
    }
}
