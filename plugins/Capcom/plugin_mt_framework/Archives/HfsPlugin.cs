using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_mt_framework.Archives
{
    public class HfsPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("5a2dfcb6-60d6-4783-acd2-bc7fb4a65f38");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.arc"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "HFS",
            Publisher = "Capcom",
            Developer = "Capcom",
            Platform = ["PS3"],
            LongDescription = "The archive resource found on PS3 games by Capcom."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "\0SFH";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new HfsState();
        }
    }
}
