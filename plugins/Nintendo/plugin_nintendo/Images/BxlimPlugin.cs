using Komponent.Contract.Enums;
using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_nintendo.Images
{
    public class BxlimPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("cf5ae49f-0ce9-4241-900c-668b5c62ce33");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.bclim", "*.bflim"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["IcySon55", "onepiecefreak"],
            Name = "BXLIM",
            Publisher = "Nintendo",
            Developer = "Nintendo",
            Platform = ["3DS", "WiiU"],
            LongDescription = "The BCLIM and BFLIM image containers used in Nintendo 3DS games or newer."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream, ByteOrder.BigEndian);

            // Read byte order
            fileStream.Position = fileStream.Length - 0x24;
            var byteOrder = (ByteOrder)br.ReadUInt16();

            // Read header
            br.ByteOrder = byteOrder;
            fileStream.Position = fileStream.Length - 0x28;

            var magic = br.ReadString(4);
            br.BaseStream.Position += 8;

            var fileSize = br.ReadInt32();

            return magic is "CLIM" or "FLIM" && fileSize == fileStream.Length;
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new BxlimState();
        }
    }
}
