using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_atlus.Image
{
    public class TmxPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("c533c2a1-4fdb-4e2a-bbb5-c07d6bf5a22d");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.tmx"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "TMX",
            Publisher = "Atlus",
            Developer = "Atlus",
            Platform = ["3DS"],
            LongDescription = "An image resource from Atlus games."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);

            fileStream.Position = 8;
            return br.ReadString(4) == "TMX0";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager fileManager)
        {
            return new TmxState();
        }
    }
}
