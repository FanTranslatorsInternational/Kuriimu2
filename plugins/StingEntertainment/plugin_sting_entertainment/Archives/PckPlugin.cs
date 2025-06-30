using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_sting_entertainment.Archives
{
    public class PckPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("dcdd3e5f-ba74-4541-a870-5d705ed6471a");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.pck"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "PCK",
            Publisher = "Atlus",
            Developer = "Sting Entertainment",
            Platform = ["Vita"],
            LongDescription = "The main package resource in Dungeon Travelers 2."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(8) == "Filename";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new PckState();
        }
    }
}
