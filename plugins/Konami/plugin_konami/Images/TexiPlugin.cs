using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_konami.Images
{
    public class TexiPlugin : IDeprecatedFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("254ee8f3-5472-492f-8726-9672653e765e");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.tex"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "TEXI",
            Publisher = "Konami",
            Developer = "Konami",
            Platform = ["3DS"],
            LongDescription = "The image resource in New Love Plus."
        };

        public DeprecatedPluginAlternative[] Alternatives { get; } =
        [
            new() { ToolName = "Kuriimu 1", Url = "https://github.com/IcySon55/Kuriimu" }
        ];

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            string texiPath = filePath + "i";
            if (fileSystem.FileExists(texiPath))
                return false;

            Stream fileStream = await fileSystem.OpenFileAsync(texiPath);

            using var br = new BinaryReaderX(fileStream);

            br.BaseStream.Position = 0x18;
            return br.ReadString(4) is "SERI";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            throw new NotImplementedException();
        }
    }
}
