using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_spike_chunsoft.Images
{
    public class CtePlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("70d8a0ef-0aad-4ef6-b219-aecee241f01c");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.img"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "CTE",
            Publisher = "Nintendo",
            Developer = "Spike Chunsoft",
            Platform = ["3DS"],
            LongDescription = "One image resource in Pokemon Super Mystery Dungeon."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "\0cte";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new CteState();
        }
    }
}
