using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_nintendo.Archives
{
    public class NcchPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("7d0177a6-1cab-44b3-bf22-39f5548d6cac");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.cxi", "*.cfa"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "NCCH",
            Publisher = "Nintendo",
            Developer = "Nintendo",
            Platform = ["3DS"],
            LongDescription = "3DS Content Container."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br=new BinaryReaderX(fileStream);

            fileStream.Position = 0x100;
            return br.ReadString(4) == "NCCH";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new NcchState();
        }
    }
}
