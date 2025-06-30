using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Plugin.File;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;

namespace plugin_nintendo.Archives
{
    public class WiiDiscPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("e5a2f369-2daa-4575-ae4f-f980aac8f2c3");

        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => ["*.cgrp"];

        public PluginMetadata Metadata { get; } = new()
        {
            Author = ["onepiecefreak"],
            Name = "WiiDisc",
            Publisher = "Nintendo",
            Developer = "Nintendo",
            Platform = ["Wii"],
            LongDescription = "The disc format for the Nintendo Wii."
        };

        public IFilePluginState CreatePluginState(IPluginFileManager pluginFileManager)
        {
            return new WiiDiscState();
        }
    }
}
