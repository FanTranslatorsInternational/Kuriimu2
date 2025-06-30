using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_level5.N3DS.Image
{
    public class ZtexPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("e131dd95-a61b-4eee-a4fa-48d222ac03d5");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.ztex"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "ZTEX",
            Publisher = "Level5",
            Developer = "Level5",
            Platform = ["3DS"],
            LongDescription = "The main image resource in Fantasy Life."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "ztex";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager fileManager)
        {
            return new ZtexState();
        }
    }
}
