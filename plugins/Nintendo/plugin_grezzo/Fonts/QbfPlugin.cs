using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_grezzo.Fonts
{
    public class QbfPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("b4353a6d-69de-4315-9931-75f479614e03");
        public PluginType PluginType => PluginType.Font;
        public string[] FileExtensions => ["*.qbf"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "QBF",
            Publisher = "Nintendo",
            Developer = "Grezzo",
            Platform = ["3DS"],
            LongDescription = "Main font resource in Zelda 3D: Ocarina of Time"
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            await using Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) is "QBF1";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new QbfState();
        }
    }
}
