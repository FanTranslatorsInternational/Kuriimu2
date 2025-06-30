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
    public class AifPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("eab51bcd-385b-4b06-b622-2a433cfc4530");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.aif"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "AIF",
            Publisher = "Level5",
            Developer = "Level5",
            Platform = ["3DS"],
            LongDescription = "Main image resource in Danball Senki by Level5."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == " FIA";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager fileManager)
        {
            return new AifState();
        }
    }
}
