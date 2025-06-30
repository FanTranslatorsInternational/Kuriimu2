using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_blue_reflection.Images
{
    class KsltPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("69D27048-0EA2-4C48-A9A3-19521C9115C3");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.kslt"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["Megaflan"],
            Name = "KSLT",
            Publisher = "Koei Tecmo",
            Developer = "Gust",
            Platform = ["Vita"],
            LongDescription = "This is the KSLT image adapter for Kuriimu2."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(8) == "TLSK3100";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new KsltState();
        }
    }
}
