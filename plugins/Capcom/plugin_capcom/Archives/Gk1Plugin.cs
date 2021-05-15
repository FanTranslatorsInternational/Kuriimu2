using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_capcom.Archives
{
    public class Gk1Plugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("5e7d1d34-4106-4d72-8d69-773b2713ae46");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.bin" };
        public PluginMetadata Metadata { get; }

        public Gk1Plugin()
        {
            Metadata = new PluginMetadata("GK1", "onepiecefreak", "The main resource archive in Gyakuten Kenji 1.");
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new Gk1State();
        }
    }
}
