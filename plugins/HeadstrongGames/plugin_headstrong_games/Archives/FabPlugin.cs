using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_headstrong_games.Archives
{
    public class FabPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("c112dde7-b983-4c63-9c06-9e4fbfee04d5");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.fab"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "FAB",
            Publisher = "Headstrong Games",
            Developer = "Headstrong Games",
            Platform = ["3DS"],
            LongDescription = "The main file resource in Pokemon Art Academy."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            var magic = br.ReadString(4);

            fileStream.Position += 0x4;
            var magic2 = br.ReadString(4);

            return magic == "FBRC" && magic2 == "BNDL";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new FabState();
        }
    }
}
