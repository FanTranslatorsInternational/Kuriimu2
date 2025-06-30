using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_bandai_namco.Archives
{
    public class L7cPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("067f4da2-98b5-43f5-9698-d77c81184642");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.l7c"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "L7C",
            Publisher = "Bandai Namco",
            Developer = "Bandai Namco",
            Platform = ["Vita"],
            LongDescription = "The resource archive in Tales Of games on PS Vita."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "L7CA";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new L7cState();
        }
    }
}
