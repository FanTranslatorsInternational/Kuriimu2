using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Assembly;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_khronos_group.Images
{
    public class KtxPlugin : IIdentifyFiles
    {
        private static readonly IList<byte[]> SupportedMagics = new List<byte[]>
        {
            /* KTX 11 */ new byte[]{0xAB, 0x4B, 0x54, 0x58, 0x20, 0x31, 0x31, 0xBB, 0x0D, 0x0A, 0x1A, 0x0A}
        };

        public Guid PluginId => Guid.Parse("d25919cc-ac22-4f4a-94b2-b0f42d1123d4");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.ktx"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["Nominom", "onepiecefreak"],
            Name = "KTX",
            Publisher = "Khronos Group",
            Developer = "Khronos Group",
            Platform = ["Android"],
            LongDescription = "The image resource by the Khronos Group."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            byte[] magic = br.ReadBytes(12);

            return SupportedMagics.Any(x => x.SequenceEqual(magic));
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new KtxState();
        }

        public void RegisterAssemblies(IAssemblyManager manager)
        {
            manager.FromResource("plugin_khronos_group.Libs.BCnEncoder.dll");
        }
    }
}
