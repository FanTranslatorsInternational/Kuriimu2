using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_kadokawa.Images
{
    public class CtxPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("b792e3e9-b8ee-431d-98ff-1c0a81155dc6");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.ctx"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "CTX",
            Publisher = "Kadokawa",
            Developer = "Kadokawa",
            Platform = ["3DS"],
            LongDescription = "The main image resource in 3DS Kadokawa games, e.g Highschool DxD"
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(8) == "CTX 10 \0";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new CtxState();
        }
    }
}
