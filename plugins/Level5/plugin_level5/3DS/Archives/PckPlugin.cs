using System;
using Kontract.Interfaces.Managers.Files;
using Kontract.Interfaces.Plugins.Entry;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.Plugins.Entry;

namespace plugin_level5._3DS.Archives
{
    public class PckPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("65178a15-caf5-4f3f-8ece-beb3e4308d0c");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.pck" };
        public PluginMetadata Metadata { get; }

        public PckPlugin()
        {
            Metadata = new PluginMetadata("PCK", "onepiecefreak", "General game archive for 3DS Level-5 games");
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new PckState();
        }
    }
}
