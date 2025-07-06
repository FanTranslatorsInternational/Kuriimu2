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
    public class MtArcPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("5a2dfcb6-60d6-4783-acd1-bc7fb4a65f38");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.arc"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "MT ARC",
            Publisher = "Capcom",
            Developer = "Capcom",
            Platform = ["3DS", "PS3", "Android", "Switch"],
            LongDescription = "The main archive resource in Capcom games using the MT Framework."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            string magic = br.ReadString(4);
            return magic is "ARC\0" or "\0CRA";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new MtArcState();
        }
    }
}
