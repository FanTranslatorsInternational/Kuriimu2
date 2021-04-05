using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_sega.Images
{
    public class CompPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("7de736f9-906e-4d1f-823c-b4f189885b6e");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.comp" };
        public PluginMetadata Metadata { get; }

        public CompPlugin()
        {
            Metadata = new PluginMetadata("COMP", "onepiecefreak", "The image resource found Sega games from the 3DS.");
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new CompState();
        }
    }
}
