using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_arc_system_works.Images
{
    public class CvtPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("73968c1e-a7f0-402d-9178-5eabeab8b2a8");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.cvt"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "CVT",
            Publisher = "Arc System Works",
            Developer = "Arc System Works",
            Platform = ["3DS"],
            LongDescription = "The main image resource in Chase: Cold Case Investigations."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(2) == "n\0";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new CvtState();
        }
    }
}
