using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_level5._3DS.Archives
{
    public class FLPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("1d6586c6-2a42-4899-8a81-f2ed4cb053d3");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] {"*.bin"};
        public PluginMetadata Metadata { get; }

        public FLPlugin()
        {
            Metadata=new PluginMetadata("FLBin","onepiecefreak","The main archive resource in Fantasy Life");
        }

        public IPluginState CreatePluginState(IFileManager fileManager)
        {
            return new FLState();
        }
    }
}
