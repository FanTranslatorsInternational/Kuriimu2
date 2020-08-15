using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace Kore.Models.UnsupportedPlugin
{
    public class HexPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("00000000-0000-0000-0000-000000000000");
        public PluginType PluginType => PluginType.Hex;
        public string[] FileExtensions => Array.Empty<string>();
        public PluginMetadata Metadata { get; }

        public HexPlugin()
        {
            Metadata = new PluginMetadata("Default", "onepiecefreak", "No description");
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new HexState();
        }
    }
}
