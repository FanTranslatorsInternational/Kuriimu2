using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_yuusha_shisu.MSG
{
    public class MsgPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("24b5902e-c38c-49ad-b7c9-283636d686bd");
        public PluginType PluginType => PluginType.Text;
        public string[] FileExtensions => new[] { "*.bin" };
        public PluginMetadata Metadata { get; }

        public MsgPlugin()
        {
            Metadata = new PluginMetadata("MSG", "StorMyu", "Death of a Hero");
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new MsgState();
        }
    }
}
