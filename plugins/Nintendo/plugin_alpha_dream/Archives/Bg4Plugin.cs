using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_alpha_dream.Archives
{
    public class Bg4Plugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("e50815d5-d54e-489e-b6ec-9c023d418305");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.dat"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "BG4",
            Publisher = "Nintendo",
            Developer = "AlphaDream",
            Platform = ["3DS"],
            LongDescription = "The main resource archive in Mario & Luigi Superstar Saga."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(3) == "BG4";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new Bg4State();
        }
    }
}
