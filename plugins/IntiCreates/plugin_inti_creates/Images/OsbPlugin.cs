using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_inti_creates.Images
{
    public class OsbPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("2f9faa67-afd0-4209-a2a5-b67974bb9a03");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] {"*.osbctr", "*.osb"};
        public PluginMetadata Metadata { get; }

        public OsbPlugin()
        {
            Metadata = new PluginMetadata("OSB", "onepiecefreak", "The main image resource for IntiCreate games.");
        }

        public IPluginState CreatePluginState(IBaseFileManager fileManager)
        {
            return new OsbState();
        }
    }
}
