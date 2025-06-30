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
    public class IdxPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("082d58ca-f3c6-4bb7-ae9a-b46b97a6bb43");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.idx"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "IDX",
            Publisher = "Bandai Namco",
            Developer = "Bandai Namco",
            Platform = ["3DS"],
            LongDescription = "Main package resource in Gundam 3D Battle."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            fileStream.Position = 0x10;

            var headerCount = 0;
            while (br.ReadString(8) == ApkSection.PackHeader)
            {
                headerCount++;
                fileStream.Position += 0x28;
            }

            return headerCount == 5;
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new IdxState();
        }
    }
}
