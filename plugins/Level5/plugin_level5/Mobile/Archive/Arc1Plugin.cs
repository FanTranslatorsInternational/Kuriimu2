using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_level5.Mobile.Archive
{
    public class Arc1Plugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("e499eb38-f6b0-4bc8-a846-0ea73cf2907a");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.obb"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "ARC1",
            Publisher = "Level5",
            Developer = "Level5",
            Platform = ["Android"],
            LongDescription = "Main data of Professor Layton 1 on Android."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            byte[] buffer = br.ReadBytes(4);

            return buffer[0] is 0xB4 && buffer[1] is 0x11 && buffer[2] is 0xC2 && buffer[3] is 0x02;
        }

        public IFilePluginState CreatePluginState(IPluginFileManager fileManager)
        {
            return new Arc1State();
        }
    }
}
