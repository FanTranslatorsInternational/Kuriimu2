using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_ruby_party.Archives
{
    public class PaaPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("cd294c2b-964c-4a05-8a8b-36387d8f8bc7");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.bin"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "PAA",
            Publisher = "Koei Tecmo",
            Developer = "Ruby Party",
            Platform = ["Vita"],
            LongDescription = "The main archive in Angelique Retour."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(3) == "PAA";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new PaaState();
        }
    }
}
