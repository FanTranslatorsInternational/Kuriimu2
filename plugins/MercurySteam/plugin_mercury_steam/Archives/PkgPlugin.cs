using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_mercury_steam.Archives
{
    public class PkgPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("63df2b3c-2763-435e-a289-a8444ef1da0d");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.pkg"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "PKG",
            Publisher = "MercurySteam",
            Developer = "MercurySteam",
            Platform = ["3DS"],
            LongDescription = "The main archive resource in Metroid: Samus Returns."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new PkgState();
        }
    }
}
