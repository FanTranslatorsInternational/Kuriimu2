using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_metal_max.Archives
{
    public class PakPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("2a4e1bf2-1718-44bd-8a72-8c33a9026fb8");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.pak" };
        public PluginMetadata Metadata { get; }

        public PakPlugin()
        {
            Metadata = new PluginMetadata("PAK", "onepiecefreak", "The main resource in Metal Max 3.");
        }

        public IPluginState CreatePluginState(IBaseFileManager pluginManager)
        {
            return new PakState();
        }
    }
}
