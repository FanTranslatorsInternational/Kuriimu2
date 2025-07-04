using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_tri_ace.Archives
{
    public class PackPlugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("8c81d937-e1a8-42e6-910a-d9911a6a93af");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.bin", "*.pack"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "P@CK",
            Publisher = "Konami",
            Developer = "tri-Ace",
            Platform = ["3DS"],
            LongDescription = "The P@CK archive for Beyond The Labyrinth."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext context)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "P@CK";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new PackState();
        }
    }
}
