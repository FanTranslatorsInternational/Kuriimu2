using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_tamsoft.Archives
{
    public class SkbPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("d0e36110-815c-45d9-9371-63ca258fc358");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.bin" };
        public PluginMetadata Metadata { get; }

        public SkbPlugin()
        {
            Metadata = new PluginMetadata("SKB", "onepiecefreak", "The main resource archive used in Senran Kagura Burst on 3DS.");
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new SkbState();
        }
    }
}
