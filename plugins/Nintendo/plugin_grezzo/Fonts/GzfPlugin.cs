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
    public class GzfPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("00ac9f14-ae2e-429c-aa3f-a3032c8c85da");
        public PluginType PluginType => PluginType.Font;
        public string[] FileExtensions => ["*.gzf"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "GZF",
            Publisher = "Nintendo",
            Developer = "Grezzo",
            Platform = ["3DS"],
            LongDescription = "Main font resource in Zelda 3D: Majora's Mask"
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            await using Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) is "GZFX";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new GzfState();
        }
    }
}
