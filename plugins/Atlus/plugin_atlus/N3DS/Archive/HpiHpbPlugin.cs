using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;
using Konnect.Extensions;

namespace plugin_atlus.N3DS.Archive
{
    public class HpiHpbPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("9479a384-5725-47c0-9257-0f3f88fdbcde");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.hpi", "*.hpb"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "HPIHPB",
            Publisher = "Atlus",
            Developer = "Atlus",
            Platform = ["3DS"],
            LongDescription = "The main archive for Etrian Odyssey games on 3DS."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var hpiPath = filePath;
            if (filePath.GetExtensionWithDot() == ".HPB")
                hpiPath = filePath.ChangeExtension("HPI");

            if (!fileSystem.FileExists(hpiPath))
                return false;

            var fileStream = await fileSystem.OpenFileAsync(hpiPath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "HPIH";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager fileManager)
        {
            return new HpiHpbState();
        }
    }
}
