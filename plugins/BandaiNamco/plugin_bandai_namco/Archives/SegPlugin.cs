using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_bandai_namco.Archives
{
    public class SegPlugin:IFilePlugin
    {
        public Guid PluginId { get; }
        public PluginType PluginType { get; }
        public string[] FileExtensions { get; }
        public PluginMetadata Metadata { get; }

        public SegPlugin()
        {
            Metadata=new PluginMetadata("SEG","onepiecefreak","The SEG format in Super Robot Taisen Z.");
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new SegState();
        }
    }
}
