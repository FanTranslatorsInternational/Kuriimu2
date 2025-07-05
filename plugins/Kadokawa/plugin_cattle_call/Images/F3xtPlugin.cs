using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_cattle_call.Images
{
    public class F3xtPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("36da51fd-c837-45e6-8350-1c295618bc2a");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.tex"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "F3XT",
            Publisher = "Kadokawa",
            Developer = "Cattle Call",
            Platform = ["3DS"],
            LongDescription = "The image resource in Metal Max 4."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            var magic1 = br.ReadString(4);

            fileStream.Position++;
            var magic2 = br.ReadString(4);

            return magic1 == "F3XT" || magic2 == "F3XT";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new F3xtState();
        }
    }
}
