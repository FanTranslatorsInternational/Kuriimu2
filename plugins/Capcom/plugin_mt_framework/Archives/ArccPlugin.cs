using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_mt_framework.Archives
{
    public class ArccPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("5a2dfcb6-60d6-4783-acd3-bc7fb4a65f38");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.arc"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "ARCC",
            Publisher = "Capcom",
            Developer = "Capcom",
            Platform = ["Android"],
            LongDescription = "The encrypted archive resource found on mobile Capcom games."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "ARCC";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new ArccState();
        }
    }
}
