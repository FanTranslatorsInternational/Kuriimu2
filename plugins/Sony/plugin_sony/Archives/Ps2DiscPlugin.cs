using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_sony.Archives
{
    public class Ps2DiscPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("c774f77b-4fe4-4550-9ca0-c8967b99eb78");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.iso" };
        public PluginMetadata Metadata { get; }

        public Ps2DiscPlugin()
        {
            Metadata = new PluginMetadata("PS2Disc", "onepiecefreak", "The game disc format for all PS2 games.");
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new Ps2DiscState();
        }
    }
}
