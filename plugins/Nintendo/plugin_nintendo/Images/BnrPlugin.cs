using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_nintendo.Images
{
    public class BnrPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("f1fd5589-550d-4916-a358-4866e0e904e1");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] {"*.bnr", "*.bin"};
        public PluginMetadata Metadata { get; }

        public BnrPlugin()
        {
            Metadata=new PluginMetadata("BNR","onepiecefreak","The DS Banner format.");
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new BnrState();
        }
    }
}
