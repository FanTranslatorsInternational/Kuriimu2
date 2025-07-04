using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_konami.Archives
{
    public class TarcPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("f7d52572-b076-4f0d-b7c2-533984428d20");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.tarc"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "TARC",
            Publisher = "Konami",
            Developer = "Konami",
            Platform = ["3DS"],
            LongDescription = "The main resource in Tongari Boushi."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "TBAF";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new TarcState();
        }
    }
}
