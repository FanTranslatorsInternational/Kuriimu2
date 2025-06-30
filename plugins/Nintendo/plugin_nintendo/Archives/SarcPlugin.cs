using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_nintendo.Archives
{
    public class SarcPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("1be80d18-e44e-43d6-884a-65d0b42bfa20");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.szs", "*.arc", "*.sblarc", "*.zlib"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "SARC",
            Publisher = "Nintendo",
            Developer = "Nintendo",
            Platform = ["3DS", "WiiU"],
            LongDescription = "A main archive resource in Nintendo games."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            if (br.PeekString(4) == "SARC")
                return true;

            fileStream.Position = 0x11;
            if (br.PeekString(4) == "SARC")
                return true;

            await using var zlibStream = new InflaterInputStream(new SubStream(fileStream, 4, fileStream.Length - 4)) {IsStreamOwner = false};
            using var zlibBr = new BinaryReaderX(zlibStream);

            return zlibBr.ReadString(4) == "SARC";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new SarcState();
        }
    }
}
