using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_nintendo.Images
{
    public class RawJtexPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("dcac8fbe-6911-43ac-a7df-cda5485743e3");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.jtex" };
        public PluginMetadata Metadata { get; }

        public RawJtexPlugin()
        {
            Metadata = new PluginMetadata("RawJTEX", "onepiecefreak", "The image format used in 3DS games.");
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new RawJtexState();
        }
    }
}
