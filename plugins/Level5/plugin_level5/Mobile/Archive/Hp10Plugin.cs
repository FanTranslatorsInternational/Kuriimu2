using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_level5.Mobile.Archive
{
    public class Hp10Plugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("1a2aec5f-568c-43e8-8fa0-7178ded1a39d");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.obb"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "HP10",
            Publisher = "Level5",
            Developer = "Level5",
            Platform = ["Android"],
            LongDescription = "Main data of Lady Layton on Android."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br=new BinaryReaderX(fileStream);
            return br.ReadString(4) == "HP10";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager fileManager)
        {
            return new Hp10State();
        }
    }
}
