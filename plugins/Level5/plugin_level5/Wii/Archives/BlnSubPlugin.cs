using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_level5.Wii.Archives
{
    public class BlnSubPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("6d71d07c-b517-496b-b659-3498cd3542fd");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.bin" };
        public PluginMetadata Metadata { get; }

        public BlnSubPlugin()
        {
            Metadata = new PluginMetadata("BLN Sub", "onepiecefreak", "Archive in Inazuma Eleven GO Strikers 2013 BLN files.");
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new BlnSubState();
        }
    }
}
