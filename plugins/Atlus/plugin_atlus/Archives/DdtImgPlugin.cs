using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_atlus.Archives
{
    public class DdtImgPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("ddf8e73e-1037-445f-b3f9-cfd2ce9cbde2");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.ddt", "*.img" };
        public PluginMetadata Metadata { get; }

        public DdtImgPlugin()
        {
            Metadata=new PluginMetadata("DDTIMG","IcySon55","Main archive of PS2 Atlus games.");
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new DdtImgState();
        }
    }
}
