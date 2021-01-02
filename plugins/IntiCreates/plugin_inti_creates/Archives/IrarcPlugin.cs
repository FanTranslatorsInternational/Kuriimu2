using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_inti_creates.Archives
{
    public class IrarcPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("9bd9e260-6e91-48cb-9603-2e0c40e06013");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] {"*.irarc", "*.irlst"};
        public PluginMetadata Metadata { get; }

        public IrarcPlugin()
        {
            Metadata = new PluginMetadata("IRARC", "onepiecefreak", "An archive for Azure Striker Gunvolt on 3DS.");
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new IrarcState();
        }
    }
}
