using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_grezzo.Images
{
    public class CtxbPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("a45653ae-15b9-46d8-8bfe-e5a44159d2b8");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.ctxb"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "CTXB",
            Publisher = "Nintendo",
            Developer = "Grezzo",
            Platform = ["3DS"],
            LongDescription = "The main image resource in 3DS Zelda ports."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "ctxb";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new CtxbState();
        }
    }
}
