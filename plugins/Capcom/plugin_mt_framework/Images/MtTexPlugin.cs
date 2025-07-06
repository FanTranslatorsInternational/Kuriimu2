using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_mt_framework.Images
{
    public class MtTexPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("9e85ef16-7157-40ba-846a-b5a17148775f");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.tex"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "MT TEX",
            Publisher = "Capcom",
            Developer = "Capcom",
            Platform = ["3DS", "Switch", "PC", "PS3", "Android"],
            LongDescription = "Main image resource for the MT Framework by Capcom."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            var magic = br.ReadString(4);
            return magic is "TEX\0" or "\0XET" or "TEX ";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new MtTexState();
        }
    }
}
