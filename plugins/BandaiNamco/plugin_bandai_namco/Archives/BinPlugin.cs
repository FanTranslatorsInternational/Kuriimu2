using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_bandai_namco.Archives
{
    public class BinPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("0476ea75-73e2-4e2f-995d-874093a3fc23");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.bin" };
        public PluginMetadata Metadata { get; }

        public BinPlugin()
        {
            Metadata = new PluginMetadata("SRTUX", "onepiecefreak", "The main resource in Kanken Training 2 and SRTUX.");
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new BinState();
        }
    }
}
