using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_nippon_ichi.Archives
{
    public class DatPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("89c7658b-6371-4be9-96b4-db9b9eb77be9");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.DAT"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "DAT",
            Publisher = "Nippon Ichi Software",
            Developer = "Nippon Ichi Software",
            Platform = ["PSP"],
            LongDescription = "Main resource in Hayarigami games."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(7) == "NISPACK";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new DatState();
        }
    }
}
