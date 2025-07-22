using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_mt_framework.Fonts
{
    public class GfdPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("e95928dd-31b9-445c-afbd-d692c694abae");
        public PluginType PluginType => PluginType.Font;
        public string[] FileExtensions => [".gfd"];
        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "GFD",
            Publisher = "Capcom",
            Developer = "Capcom",
            Platform = ["3DS"],
            LongDescription = "Main font resource for the MT Framework by Capcom."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) is "GFD\0";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new GfdState();
        }
    }
}
