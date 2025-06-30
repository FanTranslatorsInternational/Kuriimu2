using Komponent.Contract.Enums;
using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_nintendo.Archives
{
    public class U8Plugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("410009a3-49ef-4356-b9be-a7685c4f786c");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.bin"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "U8",
            Publisher = "Nintendo",
            Developer = "Nintendo",
            Platform = ["Wii"],
            LongDescription = "The main archive format for Wii games."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream, ByteOrder.BigEndian);

            return br.ReadUInt32() == 0x55aa382d;
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new U8State();
        }
    }
}
