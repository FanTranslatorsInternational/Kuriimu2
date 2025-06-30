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
    public class AmbPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("f701c40e-d7e8-4413-b3de-91eafbca450a");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.amb", "*.AMB"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "AMB",
            Publisher = "Bandai Namco",
            Developer = "Bandai Namco",
            Platform = ["3DS"],
            LongDescription = "The resource archive used in Dragon Ball Heroes games."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "#AMB";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new AmbState();
        }
    }
}
