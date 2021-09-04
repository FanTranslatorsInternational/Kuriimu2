using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_shade.Archives
{
    public class BinPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("a66defb1-bdf6-4d0a-ac7a-78eb418787ea");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.bin" };
        public PluginMetadata Metadata { get; }
        public BinPlugin() 
        {
            Metadata = new PluginMetadata("BIN", "Obluda;Alpha", "Archive in various SHADE games");
        }

        public IPluginState CreatePluginState(IBaseFileManager pluginManager)
        {
            return new BinState();
        }
    }
}
