using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_nintendo.Archives
{
    public class UMSBTPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("2546d1de-7ba9-4a1b-a809-247314c57ab5");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.umsbt" };
        public PluginMetadata Metadata { get; }

        public UMSBTPlugin()
        {
            Metadata = new PluginMetadata("UMSBT", "IcySon55; onepiecefreak", "The UMSBT resource for Nintendo games.");
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new UMSBTState();
        }
    }
}
