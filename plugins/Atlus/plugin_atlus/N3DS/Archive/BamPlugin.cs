using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_atlus.N3DS.Archive
{
    public class BamPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("f0fac450-6567-4bf6-a820-d9c32134062d");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.bam"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "BAM",
            Publisher = "Atlus",
            Developer = "Atlus",
            Platform = ["3DS"],
            LongDescription = "The BAM resource container for Atlus games on 3DS."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "ATBC";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginManager)
        {
            return new BamState();
        }
    }
}
