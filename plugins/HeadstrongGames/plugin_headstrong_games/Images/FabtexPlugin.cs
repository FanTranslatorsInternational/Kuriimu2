using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_headstrong_games.Images
{
    public class FabtexPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("7508c096-591b-44b2-b0f0-c8495b862ec0");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.fabtex"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "FABTEX",
            Publisher = "Headstrong Games",
            Developer = "Headstrong Games",
            Platform = ["3DS"],
            LongDescription = "The main image resource in Pokemon Art Academy."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            var magic = br.ReadString(4);

            fileStream.Position += 0x4;
            var magic2 = br.ReadString(4);

            return magic == "FBRC" && magic2 == "TXTR";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new FabtexState(pluginFileManager);
        }
    }
}
