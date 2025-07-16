using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_level5.NDS.Image
{
    public class GtxtPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("20341149-76dc-43a5-9c02-d87b16f8b369");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.lt", "*.lp"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "GTXT",
            Publisher = "Level5",
            Developer = "Level5",
            Platform = ["NDS"],
            LongDescription = "The main image resource in Professor Layton Spectre's Call by Level5."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br=new BinaryReaderX(fileStream);
            string magic = br.ReadString(4);

            return magic is "GTXT" or "GPLT";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager fileManager)
        {
            return new GtxtState();
        }
    }
}
