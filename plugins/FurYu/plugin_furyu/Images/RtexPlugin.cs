using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_furyu.Images
{
    public class RtexPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("4dbf4d5b-ae1d-4369-b02d-295f93fac10c");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.rtex"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "RTEX",
            Publisher = "FurYu",
            Developer = "FurYu",
            Platform = ["3DS"],
            LongDescription = "The main image resource in Gaki no Tsukai."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            return br.ReadString(4) == "RTEX";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new RtexState();
        }
    }
}
