using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_most_wanted_ent.Images
{
    public class CtgdPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("68b01e10-af37-4064-bd14-1bdcd10036ff");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.tgd", "*.ctgd"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "CTGD",
            Publisher = "Most Wanted Entertainment",
            Developer = "Most Wanted Entertainment",
            Platform = ["NDS"],
            LongDescription = "The image resource in Memory Tales Time Travel."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);

            fileStream.Position = 4;
            var magic = br.ReadString(4);

            fileStream.Position = 9;
            var magic1 = br.ReadString(4);

            return magic == "nns_" || magic1 == "nns_";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new CtgdState();
        }
    }
}
