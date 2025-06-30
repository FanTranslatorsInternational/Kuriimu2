using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;


namespace plugin_atlus.N3DS.Image
{
    public class Spr3Plugin : IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("e8df5de0-39a7-4bbe-9779-8fd687da0fe7");

        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => ["*.spr3"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "SPR3",
            Publisher = "Atlus",
            Developer = "Atlus",
            Platform = ["3DS"],
            LongDescription = "The main image resource in Persona Q games."
        };

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br=new BinaryReaderX(fileStream);
            fileStream.Position = 8;

            return br.ReadString(4) == "SPR3";
        }

        public IFilePluginState CreatePluginState(IPluginFileManager fileManager)
        {
            return new Spr3State(fileManager);
        }
    }
}
