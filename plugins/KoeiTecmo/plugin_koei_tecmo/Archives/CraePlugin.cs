using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_koei_tecmo.Archives
{
    public class CraePlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("a7d2ed59-6a9a-49a9-880f-ba206d6cf029");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.gz"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "CRAE",
            Publisher = "Koei Tecmo",
            Developer = "Koei Tecmo",
            Platform = ["Vita"],
            LongDescription = "Main archive resource in Blue Reflection."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "CRAE";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new CraeState();
        }
    }
}
