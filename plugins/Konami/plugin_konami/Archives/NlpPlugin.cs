using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_konami.Archives
{
    public class NlpPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("839e2182-87f5-47cd-adac-49c0b61113ff");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] {"*.bin"};
        public PluginMetadata Metadata { get; }

        public NlpPlugin()
        {
            Metadata=new PluginMetadata("NLP","onepiecefreak","The main resource for New Love Plus.");
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new NlpState();
        }
    }
}
