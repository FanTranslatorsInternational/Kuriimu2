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

namespace plugin_alpha_dream.Archives
{
    public class Bg4Plugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("e50815d5-d54e-489e-b6ec-9c023d418305");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.dat" };
        public PluginMetadata Metadata { get; }

        public Bg4Plugin()
        {
            Metadata = new PluginMetadata("BG4", "onepiecefreak", "The main resource archive in Mario & Luigi Superstar Saga.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(3) == "BG4";
        }

        public IPluginState CreatePluginState(IBaseFileManager pluginManager)
        {
            return new Bg4State();
        }
    }
}
