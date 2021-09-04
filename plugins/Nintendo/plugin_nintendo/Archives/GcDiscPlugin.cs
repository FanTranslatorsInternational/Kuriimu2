using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_nintendo.Archives
{
    public class GcDiscPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("5f1f5aec-a783-495b-a560-75dbb8dbd7f6");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.iso" };
        public PluginMetadata Metadata { get; }

        public GcDiscPlugin()
        {
            Metadata = new PluginMetadata("GcDisc", "onepiecefreak", "The DVD image format for GameCube.");
        }

        public IPluginState CreatePluginState(IBaseFileManager pluginManager)
        {
            return new GcDiscState();
        }
    }
}
