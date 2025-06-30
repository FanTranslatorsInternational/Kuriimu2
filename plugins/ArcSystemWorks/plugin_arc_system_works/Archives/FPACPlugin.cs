using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_arc_system_works.Archives
{
    public class FPACPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("101b0e6b-f45f-46e4-9140-98ccad9fa66b");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.pac"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "FPAC",
            Publisher = "Arc System Works",
            Developer = "Arc System Works",
            Platform = ["3DS"],
            LongDescription = "The main resource in Arc System Works games."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "FPAC";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new FPACState();
        }
    }
}
