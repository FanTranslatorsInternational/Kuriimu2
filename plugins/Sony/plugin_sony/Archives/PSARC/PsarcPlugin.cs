using Komponent.Contract.Enums;
using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_sony.Archives.PSARC
{
    public class PsarcPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("A260C29A-323B-4725-9592-737544F77C65");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.psarc"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["IcySon55"],
            Name = "PSARC",
            Publisher = "Sony",
            Developer = "Sony",
            Platform = ["PS2"],
            LongDescription = "The PlayStation archive format used on several platforms."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            var isPsarc = false;

            try
            {
                using var br = new BinaryReaderX(fileStream, ByteOrder.BigEndian);

                string magic = br.ReadString(4);
                isPsarc = magic == "PSAR";

                br.BaseStream.Position += 4;
                string compression = br.ReadString(4);
                isPsarc &= compression is "zlib" or "lzma";
            }
            catch (Exception)
            {
                // ignored
            }

            return isPsarc;
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new PsarcState();
        }
    }
}
