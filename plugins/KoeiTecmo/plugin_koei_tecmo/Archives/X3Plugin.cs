using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_koei_tecmo.Archives
{
    public class X3Plugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("68d4c5dd-ff62-43a5-a904-b550fe00a37d");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.bin"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "X3",
            Publisher = "Koei Tecmo",
            Developer = "Koei Tecmo",
            Platform = ["3DS"],
            LongDescription = "The main resource package in KoeiTecmo games."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadInt32() == 0x0133781D;
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new X3State();
        }
    }
}
