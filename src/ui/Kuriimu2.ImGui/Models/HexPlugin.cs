using System;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace Kuriimu2.ImGui.Models
{
    public class HexPlugin : IFilePlugin
    {
        public static Guid Guid = Guid.Parse("00000001-0000-0000-0000-000000000001");

        public Guid PluginId => Guid;
        public PluginType PluginType => PluginType.Hex;
        public string[] FileExtensions => [];

        public PluginMetadata Metadata { get; }

        public HexPlugin()
        {
            Metadata = new PluginMetadata
            {
                Author = ["onepiecefreak"],
                Name = "Default",
                Platform = ["PC"],
                Developer = "Various",
                LongDescription = "No description"
            };
        }

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new HexState();
        }
    }
}
