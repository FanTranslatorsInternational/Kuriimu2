using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_nintendo.Archives
{
    public class CiaPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("509a72a2-445f-4a62-8a13-7b82d773c03e");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.cia" };
        public PluginMetadata Metadata { get; }

        public CiaPlugin()
        {
            Metadata = new PluginMetadata("CIA", "onepiecefreak", "Installable 3DS game container.");
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new CiaState();
        }
    }
}
