using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_square_enix.Archives
{
    public class DpkPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("16951227-46b9-436c-9a02-1016ee6ffda3");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.dpk" };
        public PluginMetadata Metadata { get; }

        public DpkPlugin()
        {
            Metadata = new PluginMetadata("DPK", "onepiecefreak", "The main resource for Final Fantasy 1 3DS.");
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new DpkState();
        }
    }
}
